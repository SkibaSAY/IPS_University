"""
Базовая логика логгеров
"""


__author__ = 'Сластухин А.Ю.'


import traceback
import logging
from datetime import datetime
from helpers.mongo_buffer import MongoBuffer
from helpers.scheduler_ex import SchedulerEx
from .constants import WorkingStatus
from .helpers import get_target_collection

logging.basicConfig(level=logging.INFO)

#TODO: Добавить сборщик трафика в режиме реального времени
class LoggerBase:
    """ Базовый логгер """
    _buffer: MongoBuffer = None
    """ Неотправленные данные - отправляем пачками, для оптимизации коннектов """
    _name: str = None
    _last_activated_time: datetime = None
    _status: WorkingStatus = None
    _last_error: dict = None

    def __init__(self, name: str):
        self._name = name
        self._status = WorkingStatus.STOPED
        #TODO: мне не нравится, что буффер инициализируется в логгере - его стоит вынести в конструктор
        # пока ок, но потом логгеры будут писать в разные базы
        self._buffer = MongoBuffer(name=f'MongoBuffer#{name}')

    def start(self):
        raise NotImplementedError()

    def stop(self):
        self._log('Останавливаюсь')
        self._buffer.send_all()
        # Нельзя затирать статус ошибки - только перезапуском
        if self._status != WorkingStatus.HAS_ERROR:
            self._status = WorkingStatus.STOPED

    def save(self, target_collection, event_info: dict):
        self._buffer.add(target_collection, event_info)

    def get_info(self):
        return {
            'name': self._name,
            'status': {'key': self._status.value, 'name': self._status.name},
            'last_active_time': self._last_activated_time,
            'last_error': self._last_error
        }

    def _log(self, msg: str, level = logging.INFO, exc_info: bool = False):
        """ Логирования хода работ """
        alias = f'{self.__class__}("{self._name}")'
        log_msg = f'{alias}: {msg}'
        #TODO: настроить логирование в файл, чтобы уровни Error попадали в файл
        if level <= logging.INFO:
            logging.info(log_msg, exc_info=exc_info)
        else:
            logging.error(log_msg, exc_info=exc_info)

    def _log_error(self, msg = 'Что-то пошло не так', ex: Exception = None):
        self._last_error = {
            'text': str(ex),
            'traceback': traceback.format_exc()
        }
        error_msg = f'{msg}'
        self._log(error_msg, exc_info=  ex is not None)

    def _get_target_collection(self, event_info: dict) -> str:
        """ Для пакета определяет коллекцию, в которую пакет адресован """
        date_time = event_info.get('date_time')
        if date_time is None:
            raise Exception('Логгер должен переопределить "_get_target_collection" или задавать "date_time"')

        return get_target_collection(date_time)
 
class ScheduleLogger(LoggerBase):
    """ Логгер, запускаемый по расписанию """
    _scheduler = None
    _repeat_interval = None

    def __init__(self, name: str, repeat_interval: int=60) -> None:
        super().__init__(name)
        self._repeat_interval = repeat_interval

    def start(self):
        if self._status in [WorkingStatus.WAITING, WorkingStatus.HAS_ERROR, WorkingStatus.STOPED]:
            self._status = WorkingStatus.STARTING
            self._start_body()
            self._status = WorkingStatus.WAITING

    def _start_body(self):
        """ Тело метода start, то что выполняет на этапе WorkingStatus.STARTING """
        self._scheduler = SchedulerEx(self._name)
        # первый запуск на усмотрение разработчика логгера
        self._scheduler.repeat(delay=self._get_first_start_delay(),
                               interval=self._repeat_interval,
                               priority=1,
                               action=self.logging)
        self._scheduler.run_async()

    def stop(self, is_error: bool = False) -> bool:
        self._status = WorkingStatus.STOPING if not is_error else WorkingStatus.HAS_ERROR
        self._scheduler.stop()
        super().stop()

    def logging(self) -> None:
        """ Собирать данные """
        self._status = WorkingStatus.WORKING
        try:
            self._last_activated_time = datetime.now().replace(microsecond=0)

            event_info = self._get_data()
            if 'date_time' not in event_info:
                event_info['date_time'] = self._last_activated_time
            target_collection = self._get_target_collection(event_info)
            self._log(f'{event_info}')

            self.save(target_collection, event_info)
            self._status = WorkingStatus.WAITING
        except Exception as ex:
            self._log_error(ex)
            self.stop(is_error=True)

    def _get_data(self) -> dict:
        """ Основной переопределяемый метод - отчёт за временной интервал """
        raise NotImplementedError()
    
    def _get_first_start_delay(self) -> int:
        """
        Когда запустить первый сбор логов?
        В некоторых ситуациях надо сразу,в некоторых нужно подождать следующего по времени
        """
        return 0
