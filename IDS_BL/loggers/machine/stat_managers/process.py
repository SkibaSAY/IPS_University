import psutil
from helpers.absolute_stat import AbsoluteDictStat
from .base import StatManagerBase


def processes_statistics() -> dict:
    """ Временная массовая статистика по всем процессам """
    result: dict = {
        'file_descriptors_count': 0,
        'connections_count': 0,
        'threads_count': 0,
        'processes_count': 0
    }
    # Перебираем все процессы
    for proc in psutil.process_iter(['pid']):
        try:
            result['file_descriptors_count'] += len(proc.open_files())
            result['connections_count'] += len(proc.net_connections())
            result['threads_count'] += proc.num_threads()
            result['processes_count'] += 1
        except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
            continue

    return result


class ProcStatManager(StatManagerBase):
    """
    Менеджер загрузки процессов
    (?) Подходит для анализа активности процессов, отдельных пользователей, отдельных приложений
    (?) Зависит от конфигурации
    Например: хотим анализировать активность процессов chrome.exe
    """
    _proc_names: list[str] = None
    _users: list[str] = None
    _aggregate: bool = None

    def __init__(self, proc_names: list[str] = None, users: list[str] = None, aggregate: bool = True):
        """
        :param proc_names: имена процессов для анализа
            По умолчанию: все
        :type users: имена пользователей
            По умолчанию: все
        :type aggregate: bool - сагрегировать ли разные процессы в одну статистическую запись
            (?) Пример: 5 процессов chrome.exe - важно, ли что каждый из них работает?
                Потенциально удобно смотреть в савокупности
        """
        pass

    #TODO: Эта часть системы недописана
    # в будущем ожидается, что можно будет анализировать активность по кокнертным процессам
    def statistics(self) -> dict|list[dict]:
        result: list[dict] = []
        # Перебираем все процессы
        for proc in psutil.process_iter(['pid']):
            try:
                proc_info = {
                    'file_descriptors': proc.open_files(),
                    'connections': proc.net_connections(),
                    'threads': proc.num_threads,
                    'cpu_percent': proc.cpu_percent,
                    #'': proc.
                }
            except (psutil.NoSuchProcess, psutil.AccessDenied, psutil.ZombieProcess):
                continue
        return len(psutil.pids())
