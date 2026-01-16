"""
Менеджер для чтения данных из MongoDb
"""

from datetime import datetime, timedelta
from pymongo import MongoClient
from loggers.helpers import get_target_collection


class TotalViewManager:
    """ Менеджер для чтения данных TotalMachine из Mongo """
    _mongo_client: MongoClient = None
    _db = None

    def __init__(self, ):
        self._mongo_client = MongoClient("mongodb://localhost:27017/")
        self._db = self._mongo_client['test_ids']

    def _get_data_from_col(self, collection: str, query: dict) -> list[dict]:
        return [file for file in self._db[collection].find(query, {'_id':0})]

    def get_analyzed_items_by_date(self, date_time: datetime) -> list[dict]:
        """ Получить набор данных для анализа за переданную дату """
        collection = get_target_collection(date_time)
        # TODO: если изменится время - поменять, дублирование логики
        minutes_floor = date_time.minute // 10 * 10
        prepared_date_time = date_time.replace(minute=minutes_floor, second=0, microsecond=0)
        print(prepared_date_time)
        return self._get_data_from_col(collection, query={
            'date_time': {
                '$gte': prepared_date_time,
                '$lt': prepared_date_time + timedelta(minutes=10)
            }
        })

    def get_learning_items_by_hour(self, query_date_time: datetime, hour: int) -> list[dict]:
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
                result.extend(self._get_data_from_col(collection, query={
                    'date_time': {
                        '$lt': date_time - timedelta(days=1) + timedelta(minutes=10),
                    }
                }))

        return result
