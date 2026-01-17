"""
Менеджер для чтения данных из MongoDb
"""

from datetime import datetime, timedelta
from pymongo import MongoClient
from loggers.helpers import get_target_collection
from .constants import ViewType


def prepare_view_type_query(view_type: ViewType) -> dict:
    result: dict = None
    match view_type:
        case ViewType.CPU:
            #TODO: временный костыль - ram появился недавно, потому временно его выключим его, чтобы не ломать форматы анализаторов
            result = {'percent': 1,
                      #'ram': 0,
                      'times': 1,
                      'processes_stats': 1}
        case ViewType.DISK:
            result = {'disk': 1}
        case ViewType.NETWORK:
            result = {'network': 1}
        case _:
            result = {}

    # иначе можем занулить total 
    if len(result) != 0:
        # покажем дату - может быть полезна для анализатора
        result['date_time'] = 1
    return result | {'_id':0}


def after_reading_prepare(view_type: ViewType):
    #TODO: оптимальнее было бы написать pipeline и сразу из базы вытащить - сделано из-за спешки
    def add_prefix(file: dict, prefix: str) -> dict:
        new_result = {}
        result = {f'{prefix}_{key}': value for key, value in file.items()}
        for key, value in result.items():
            if isinstance(value, dict):
                # TODO: заведомо, можно оптимальнее написать, но сейчас мне надо быстро развернуть вложенности
                new_result = new_result | add_prefix(value, key)
            else:
                new_result[key] = value

        return new_result
    
    def prepare_view(file: dict, prefix: str) -> dict:
        date_time = file.pop('date_time')
        file_childs = list(file.values())[0]
        result = add_prefix(file_childs, prefix)
        result['date_time'] = date_time
        return result

    def total(file: dict) -> dict:
        return file

    def cpu(file: dict) -> dict:
        date_time = file.pop('date_time')
        result = add_prefix(file, 'cpu')
        result['date_time'] = date_time
        return result


    def disk(file: dict) -> dict:
        return prepare_view(file, 'disk')
    
    def network(file: dict) -> dict:
        return prepare_view(file, 'network')

    result_func = None
    match view_type:
        case ViewType.CPU:
            result_func = cpu
        case ViewType.DISK:
            result_func = disk
        case ViewType.NETWORK:
            result_func = network
        case _:
            result_func = total

    return result_func


class TotalViewManager:
    """ Менеджер для чтения данных TotalMachine из Mongo """
    _mongo_client: MongoClient = None
    _db = None

    def __init__(self, ):
        self._mongo_client = MongoClient("mongodb://localhost:27017/")
        self._db = self._mongo_client['test_ids']

    def _get_data_from_col(self, collection: str, fltr: dict, projection: dict) -> list[dict]:
        return [file for file in self._db[collection].find(filter=fltr,
                                                           projection=projection)]

    def get_analyzed_items_by_date(self, date_time: datetime, view_type: ViewType) -> list[dict]:
        """ Получить набор данных для анализа за переданную дату """
        collection = get_target_collection(date_time)
        # TODO: если изменится время - поменять, дублирование логики
        minutes_floor = date_time.minute // 10 * 10
        prepared_date_time = date_time.replace(minute=minutes_floor, second=0, microsecond=0)

        result = self._get_data_from_col(collection,
                                         fltr={
                                                'date_time': {
                                                    '$gte': prepared_date_time,
                                                    '$lt': prepared_date_time + timedelta(minutes=10)
                                                }
                                            },
                                         projection=prepare_view_type_query(view_type)
                                        )
        return list(map(after_reading_prepare(view_type), result))

    def get_learning_items_by_hour(self, query_date_time: datetime, hour: int, view_type: ViewType) -> list[dict]:
        """
        Получить обучающий(исторический) набор данных в разрезе часа
        (?) данные в коллекциях сагрегированы по 10 минут в течение суток
        Требуется взять все записи из коллекций в разрезе часа, исключить сегодняшние
        """
        result = []
        collections_names = self._db.list_collection_names()
        for i in range(6):
            # TODO: если изменится время - поменять, дублирование логики
            date_time = query_date_time.replace(hour=hour, minute=10*i)
            collection = get_target_collection(date_time)
            if collection in collections_names:
                files = self._get_data_from_col(collection, 
                                                fltr={'date_time': {
                                                            '$lt': date_time - timedelta(days=1) + timedelta(minutes=10)
                                                        }
                                                    },
                                                projection=prepare_view_type_query(view_type)
                                                )
                result.extend(list(map(after_reading_prepare(view_type), files)))

        return result
