"""
Логгер загрузки системы на текущей машине
Работает в 2 потока:
А.Постоянно считывает нагрузку с системы и передаёт данные в очередь на обработку
B.Читает очередь и формирует блоки для добавления в базу данных
"""

from time import sleep
from .helpers import get_gpu_usage, get_ram_usage, MachineStatManager
from ..base import ScheduleLogger


class MachineTotalLogger(ScheduleLogger):
    """ Логгер активности на текущей машине """
    _stat_manager: MachineStatManager = None

    def __init__(self) -> None:
        super().__init__(repeat_interval=60)
        self._stat_manager = MachineStatManager()
        # перед начало считывания нужно подождать 60 сек, чтобы данные выдавались актуальные данные
        sleep(60)

    def _get_data(self):
        info = {
            'cpu': self._stat_manager.cpu_stats(),
            'ram': get_ram_usage(),
            'gpu': get_gpu_usage(),
            'processes_count': self._stat_manager.get_processes_count()
        }

        print(info)

        return info
