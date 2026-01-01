"""
Логгер загрузки системы на текущей машине
Работает в 2 потока:
А.Постоянно считывает нагрузку с системы и передаёт данные в очередь на обработку
B.Читает очередь и формирует блоки для добавления в базу данных
"""

from datetime import datetime
from .helpers import get_cpu_usage, get_gpu_usage, get_ram_usage
from ..base import LoggerBase


class MachineTotalLogger(LoggerBase):
    """ Логгер активности на текущей машине """

    def __init__(self) -> None:
        super().__init__()

    def _get_data(self):
        cpu_usage = get_cpu_usage()
        ram_usage = get_ram_usage()
        gpu_usage = get_gpu_usage()

        info = {
            'time': datetime.now().replace(microsecond=0),
            'cpu': cpu_usage,
            'ram': ram_usage,
            'gpu': gpu_usage
        }

        print(info)

        return info
