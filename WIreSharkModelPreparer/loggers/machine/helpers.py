""" Вспомогательные функции """

from psutil import process_iter, NoSuchProcess, AccessDenied, ZombieProcess, virtual_memory, cpu_percent
import GPUtil
from collections import namedtuple
from ..constants import ConnectionInfo


def get_cpu_usage(interval: int = 10) -> float:
    """
    Измеряет использование cpu
    :param interval > 0: время, выделяемое на замер нагрузки
        (?) Если передан None, то сравнивается с последним запуском
    """
    return cpu_percent(interval=interval)

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
    
    for proc in sorted(process_iter(return_fields), 
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

        except (NoSuchProcess, AccessDenied, ZombieProcess):
            pass
    
    return processes_infos

def get_ram_usage():
    return virtual_memory().percent


def get_gpu_usage():
    gpus = GPUtil.getGPUs()
    if gpus:
        return gpus[0].load * 100  
    return 0  

