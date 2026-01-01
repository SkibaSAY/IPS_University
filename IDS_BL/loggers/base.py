"""
Базовая логика логгеров
"""


__author__ = 'Сластухин А.Ю.'


from threading import Thread
from time import time, sleep
from helpers.mongo_buffer import MongoBuffer
from helpers.scheduler_ex import SchedulerEx


class LoggerBase:
    """ Логгер активности на текущей машине """

    _buffer: MongoBuffer = None
    """ Неотправленные данные - отправляем пачками, для оптимизации коннектов """

    _scheduler = None
    _scheduler_task = None
    _repeat_interval = None

    def __init__(self, repeat_interval: int=60) -> None:
        self._buffer = MongoBuffer()
        self._repeat_interval = repeat_interval

    def _skip_first_run(self):
        """ Часто первый запуск тестовый, его результаты - мусор, но он нужен для задания начальнойц позиции """
        #print(self._get_data())
        pass

    def run(self):
        #self._skip_first_run()
        self._scheduler = SchedulerEx(time, sleep)
        self._scheduler.repeat(delay=0, interval=self._repeat_interval, priority=1, action=self.logging)
        self._scheduler.run_async()

    def logging(self) -> None:
        """ Собирать данные """
        info = self._get_data()
        print(f'{info}')
        self._buffer.add('1001-01-01', info)

    def _get_data(self) -> dict:
        """ Основной переопределяемый метод - отчёт за временной интервал """
        raise NotImplementedError()
