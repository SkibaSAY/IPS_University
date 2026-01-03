"""
Вспомогательные функции
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

def get_net_io_stats() -> dict:
    io_stats = psutil.disk_io_counters()
    return


def get_cpu_used_stats():
    """
    Статистика использования процессора с момента старта системы
    ctx_switches — количество переключений контекста
    interrupts — количество прерываний
    soft_interrupts — количество программных прерываний
    syscalls — количество системных вызовов. В Ubuntu всегда устанавливается значение 0.
    """
    stats = psutil.cpu_stats()
    return {
        'ctx_switches': stats.ctx_switches,
        'interrupts': stats.interrupts,
        'soft_interrupts': stats.soft_interrupts,
        'syscalls': stats.syscalls
    }


class MachineStatManager:
    """ Менеджер для работы со статистикой текущей машины """
    _cpu_times = AbsoluteDictStat(get_func=psutil.cpu_times,
                                  compute_fields=['user', 'system', 'idle'])
    _cpu_used_stats = AbsoluteDictStat(get_func=get_cpu_used_stats)

    def __init__(self):
        # первый вызов должен быть пропущен - тк возвращает мусор, дальнейшие вызовы осмыслены
        self.__format___first_init_stats(self)

    def _first_init_stats(self):
        """ Некоторые статистики зависят от первого вызова - он бесполезный, но задаёт первое значение для сравнения """
        self.get_cpu_usage()
        self.get_cpu_times()
        self._cpu_used_stats.get()

    def get_cpu_usage(self) -> float:
        """
        Измеряет использование cpu
            (?) сравнивается с последним запуском
        """
        return psutil.cpu_percent(interval=None)

    def get_cpu_times(self) -> dict:
        """
        Замеряет процессорное время(его изменение с момента первого запуска):
            (?) Позволяет экономить время при статистических опросах в цикле
            (!) Использующий контролирует интервал между запусками, от этого зависит релевантность результатов
        :return
            user - время, затрачиваемое обычными процессами
            system - время, затрачиваемое процессами, выполняющимися в режиме ядра
            idle - время, потраченное на простой (когда процессор ничего не делает)
        (?) Важно помнить, что 10 сек реального времени не означает, что максимум 10 сек:
            10 сек на каждый логический процессор, в сумме может быть cpu_count * interval
        https://docs-python.ru/packages/modul-psutil-python/ispolzovanie-resursov-os-tsp/#psutil.cpu_times

        (?) Рекомендуется использовать не реже, чем раз в 1 час - чтобы статитиска успела набежать
        """
        return self._cpu_times.get()

    def cpu_used_stats(self) -> dict:
        """
        Статистика использования процессора с момента последнего обращения
        (?) Рекомендуется использовать не реже, чем раз в 1 час - чтобы статитиска успела набежать
        """
        return self._cpu_used_stats.get()

    def cpu_stats(self) -> dict:
        stats = {
            'procent': self.get_cpu_usage(),
            'times': self.get_cpu_times(),
            'used': 
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
                fds = 
                # Если нужны и сокеты/пайпы, используйте proc.connections()
                # total_fds += len(proc.connections())
            except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
                continue
        return len(psutil.pids())
