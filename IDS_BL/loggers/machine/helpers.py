"""
Вспомогательные функции
(?) PSutils позволяет много информации про систему узнать
https://www.geeksforgeeks.org/python/psutil-module-in-python/
"""

import psutil
import GPUtil

from collections import namedtuple
from ..constants import ConnectionInfo
from helpers.absolute_stat import AbsoluteDictStat

def get_top_processes(n=10):
    """
    Топ N процессов, нагружающих систему
    (?) Подробнее https://docs-python.ru/packages/modul-psutil-python/obekt-process/
    TODO: на базе этого Proccess можно реализовать и IPS(блокировать процессы - уничтожать их)
    """
    def parse_connection(addr: namedtuple) -> tuple[str, str]:
        if addr:
            return addr.ip, addr.port
        return None, None

    processes_infos = []

    # TODO: 'io_counters' позволяет получить информацию о файлах, в которых работал процесс - и числе байт написанных и прочитанных
    return_fields = ['pid', 'name', 'cpu_percent', 'memory_percent', 'memory_info']
    sorted_fields = ['cpu_percent', 'memory_percent']
    
    for proc in sorted(psutil.process_iter(return_fields), 
                       key=lambda x: tuple(x.info[field] for field in sorted_fields),
                       reverse=True)[:n]:
        try:
            new_process_info = {}
            for field in return_fields:
                new_process_info[field] = proc.info[field]

            #memory_use
            memory_usage = new_process_info.pop('memory_info')
            new_process_info['memory_rss'] = memory_usage.rss
            new_process_info['memory_vms'] = memory_usage.vms

            # connections
            connections: list[ConnectionInfo] = new_process_info.setdefault('connections', [])
            for conn_info in proc.net_connections('inet'):
                local_addr = conn_info.laddr
                dest_addr = conn_info.raddr
                connections.append(ConnectionInfo(*parse_connection(local_addr), *parse_connection(dest_addr)))

            processes_infos.append(new_process_info)

        except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
            pass
    
    return processes_infos


def get_ram_usage():
    return psutil.virtual_memory().percent


def get_gpu_usage():
    gpus = GPUtil.getGPUs()
    if gpus:
        return gpus[0].load * 100  
    return 0  

def get_disk_usage(path = '/') -> dict:
    """
    Возвращает заагруженность диста в определённом разделе
    (?) По умолчанию возвращает для всей системы
    """
    disk_usage = psutil.disk_usage('/')
    return {
        'total': disk_usage.total,
        'used': disk_usage.used,
        'free': disk_usage.free,
        'precent': disk_usage.percent
    }


def get_disk_io_stats() -> dict:
    """ Статистика работы с диском с момент старта системы """
    disk_io_stats = psutil.disk_io_counters()
    return {
        'read_count': disk_io_stats.read_count,
        'read_bytes': disk_io_stats.read_bytes,
        'read_time': disk_io_stats.read_time,
        'write_count': disk_io_stats.write_count,
        'write_bytes': disk_io_stats.write_bytes,
        'write_time': disk_io_stats.write_time
    }


def get_net_io_stats() -> dict:
    """
    Статистика сетевых обращений с момента старта системы
        bytes_sent - количество отправленных байтов
        bytes_recv - количество полученных байтов
        packets_sent - количество отправленных пакетов
        packets_recv - количество полученных пакетов
        errin - общее количество ошибок при получении
        errout - общее количество ошибок при отправке
        dropin - общее количество входящих пакетов, которые были отброшены.
        dropout — общее количество отброшенных исходящих пакетов.
    """
    net_io_stats = psutil.net_io_counters()
    return {
        'bytes_sent': net_io_stats.bytes_sent,
        'bytes_recv': net_io_stats.bytes_recv,
        'packets_sent': net_io_stats.packets_sent,
        'packets_recv': net_io_stats.packets_recv,
        'errin': net_io_stats.errin,
        'errout': net_io_stats.errout,
        'dropin': net_io_stats.dropin,
        'dropout': net_io_stats.dropout
    }


def get_sensors_temperatures() -> dict:
    """
    Аппаратные температуры системы в градусах Цельсия
    (?) Only LINUX
    """
    return psutil.sensors_temperatures()


class MachineStatManager:
    """ Менеджер для работы со статистикой текущей машины """

    def __init__(self):
        # первый вызов должен быть пропущен - тк возвращает мусор, дальнейшие вызовы осмыслены
        self.__format___first_init_stats(self)

    def _first_init_stats(self):
        """ Некоторые статистики зависят от первого вызова - он бесполезный, но задаёт первое значение для сравнения """
        self.get_cpu_usage()
        self.get_cpu_times()
        self._cpu_used_stats.get()


    def cpu_stats(self) -> dict:
        stats = {
            'procent': self.get_cpu_usage(),
            'times': self.get_cpu_times(),
            'used': self.cpu_used_stats()
        }

    def io_stats(self) -> dict:
        pass

    def get_total_processes_stats(self) -> dict:
        """
        Число процессов
        потоков
        активных соединений
        файловых дескрипторов
        """

    def get_processes_stats(self) -> list[dict]:
        result: list[dict] = []
        # Перебираем все процессы
        for proc in psutil.process_iter(['pid']):
            try:
                proc_info = {
                    'file_descriptors': proc.open_files(),
                    'connections': proc.net_connections(),
                    'streams': proc.stre
                }
                # Получаем дескрипторы для каждого процесса
                # Это возвращает список объектов FileDescriptor
                #fds = 
                # Если нужны и сокеты/пайпы, используйте proc.connections()
                # total_fds += len(proc.connections())
            except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
                continue
        return len(psutil.pids())
