"""
Логгер загрузки системы на текущей машине
Работает в 2 потока:
А.Постоянно считывает нагрузку с системы и передаёт данные в очередь на обработку
B.Читает очередь и формирует блоки для добавления в базу данных
"""


from queue import Queue
import time
import datetime
from .helpers import get_cpu_usage, get_gpu_usage, get_ram_usage, get_top_processes


class Machine_Logger:
    """ Логгер активности на текущей машине """
    _args = None

    _dirty_logs: Queue = None
    """ Необработанные логи """

    def __init__(self, args: dict = None) -> None:
        self._args = args or {}
        self._dirty_logs = Queue()
    
    def start() -> None:
        """ Точка входа """
    
    def log(self) -> None:
        """ Сборщик данных """
        report_data = []
        while True:
            cur_time = datetime.now()
            cpu_usage = get_cpu_usage()
            ram_usage = get_ram_usage()
            gpu_usage = get_gpu_usage()
            
            report_data.append({
                'time': datetime.now(),
                'cpu': cpu_usage,
                'ram': ram_usage,
                'gpu': gpu_usage
            })

            time.sleep(60)

    def dirty_listener(self) -> None:
        """ Слушатель очереди - обработчик грязных данных """
        while True:
            if task := self._dirty_logs.get():
                pass
            else:
                time.sleep()

get_top_processes(10)
