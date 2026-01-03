"""
Базовая логика логгеров
"""


__author__ = 'Сластухин А.Ю.'


from datetime import datetime
from helpers.mongo_buffer import MongoBuffer
from helpers.scheduler_ex import SchedulerEx


#TODO: Добавить сборщик трафика в режиме реального времени
class LoggerBase:
    """ Базовый логгер """
    _buffer: MongoBuffer = None
    """ Неотправленные данные - отправляем пачками, для оптимизации коннектов """
    def __init__(self):
        self._buffer = MongoBuffer()
    
    def start(self):
        raise NotImplementedError()
    
    def save(self, target_collection, event_info: dict):
        self._buffer.add(target_collection, event_info)
    
    def _get_target_collection(self, event_info: dict) -> str:
        """ Для пакета определяет коллекцию, в которую пакет адресован """
        date_time = event_info.get('date_time')
        if date_time is None:
            raise Exception('Логгер должен переопределить "_get_target_collection" или задавать "date_time"')

        # округляем до 10 мин
        minutes_floor = date_time.minute // 10 * 10
        return f'time_{date_time.replace(minute=minutes_floor).strftime("%H_%M")}'
 
class ScheduleLogger(LoggerBase):
    """ Логгер, запускаемый по расписанию """
    _scheduler = None
    _scheduler_task = None
    _repeat_interval = None

    def __init__(self, repeat_interval: int=60) -> None:
        super().__init__()
        self._repeat_interval = repeat_interval

    def start(self):
        self._scheduler = SchedulerEx()
        self._scheduler.repeat(delay=0, interval=self._repeat_interval, priority=1, action=self.logging)
        self._scheduler.run_async()

    def _get_target_collection(self, event_info: dict) -> str:
        """ Для пакета определяет коллекцию, в которую пакет адресован """
        # округляем до 10 мин
        date_time = event_info['date_time']
        minutes_floor = date_time.minute // 10 * 10
        return f'time_{date_time.replace(minute=minutes_floor).strftime("%H_%M")}'

    def logging(self) -> None:
        """ Собирать данные """
        cur_date = datetime.now().replace(microsecond=0)
        event_info = self._get_data()
        if 'date_time' not in event_info:
            event_info['date_time'] = cur_date
        target_collection = self._get_target_collection(event_info)

        print(f'{event_info}')

        self.save(target_collection, event_info)

    def _get_data(self) -> dict:
        """ Основной переопределяемый метод - отчёт за временной интервал """
        raise NotImplementedError()
