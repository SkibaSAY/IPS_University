"""
Логгер загрузки системы на текущей машине
Работает в 2 потока:
А.Постоянно считывает нагрузку с системы и передаёт данные в очередь на обработку
B.Читает очередь и формирует блоки для добавления в базу данных
"""

from time import sleep
from loggers.machine.stat_managers import CPUStatManager
from ..base import ScheduleLogger


class MachineTotalLogger(ScheduleLogger):
    """ Логгер активности на текущей машине """
    _cpu_manager: CPUStatManager = None
    _repeat_interval: int = None

    def __init__(self) -> None:
        self._repeat_interval = 60
        super().__init__('TotalMachine', self._repeat_interval)

    def _start_body(self):
        # При инцииализации было зафиксировано состояние, растущих характеристик
        self._cpu_manager = CPUStatManager()
        # перед начало считывания нужно подождать первый интервал, чтобы накопились данные между первым и вторым
        super()._start_body()

    def _get_data(self):
        info = self._cpu_manager.all_stats()
        return info

    def _get_first_start_delay(self):
        # перед начало считывания нужно подождать первый интервал, чтобы накопились данные между первым и вторым
        return self._repeat_interval
