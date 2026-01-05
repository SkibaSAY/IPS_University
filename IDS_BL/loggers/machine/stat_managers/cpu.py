import psutil
from helpers.absolute_stat import AbsoluteDictStat
from .base import StatManagerBase
from .process import processes_statistics
from ..helpers import get_net_io_stats, get_disk_io_stats


class CPUStatManager(StatManagerBase):
    """ Менеджер для накопления статистики работы процессора в системе """
    _cpu_times: AbsoluteDictStat = None
    _cpu_used_stats: AbsoluteDictStat = None
    _processes_stats: AbsoluteDictStat = None
    _disk_io_stats: AbsoluteDictStat = None
    _net_io_stats: AbsoluteDictStat = None

    def _init_absolute_counters(self):
        """
        Инициализация счетчиков временнЫх изменений счётчиков с момента старта
        (?) Такие счётчки копятся с момента старта системы - для анализа удобно считать их изменение со временем
        """
        self._cpu_times = AbsoluteDictStat(get_func=self.system_times,
                                           compute_fields=['user', 'system', 'idle'])
        self._cpu_used_stats = AbsoluteDictStat(get_func=self.system_used_stats)

        #TODO: вынести, перечисленные ниже характеристики в отдельные менеджеры, по сущностям(Network, IO, Processes)
        self._processes_stats = AbsoluteDictStat(get_func=processes_statistics)
        self._disk_io_stats = AbsoluteDictStat(get_func=get_disk_io_stats)
        self._net_io_stats = AbsoluteDictStat(get_func= get_net_io_stats)

    def _first_init_stats(self):
        """ Некоторые статистики зависят от первого вызова - он бесполезный, но задаёт первое значение для сравнения """
        self.usage_percent()
        self.times()
        self.used_stats()
        self._disk_io_stats.get()
        self._net_io_stats.get()

    def __init__(self):
        self._init_absolute_counters()
        # первый вызов должен быть пропущен - тк возвращает мусор, дальнейшие вызовы осмыслены
        self._first_init_stats()

    @staticmethod
    def system_times() -> dict:
        """
        Процессорное время(с момента старта системы):
        :return
            user - время, затрачиваемое обычными процессами
            system - время, затрачиваемое процессами, выполняющимися в режиме ядра
            idle - время, потраченное на простой (когда процессор ничего не делает)
        (?) Важно помнить, что 10 сек реального времени не означает, что максимум 10 сек:
            10 сек на каждый логический процессор, в сумме может быть cpu_count * interval
        https://docs-python.ru/packages/modul-psutil-python/ispolzovanie-resursov-os-tsp/#psutil.cpu_times
        """
        times = psutil.cpu_times()
        return {
            'user': times.user,
            'system': times.system,
            'idle': times.idle
        }

    @staticmethod
    def system_used_stats():
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

    @staticmethod
    def freq() -> dict:
        """
        Частота процессора
        current
        min
        max
        (?) Для windows показывает только паспортные значения
        """
        #TODO: Для Windows всегда 1 процессор - общий, для LINUX можно рассматривать все
        freq = psutil.cpu_freq()

        return {
            'current': freq.current,
            'min': freq.min,
            'max': freq.max
        }

    def usage_percent(self) -> float:
        """
        Измеряет использование cpu
            (?) сравнивается с последним запуском
        """
        return psutil.cpu_percent(interval=None)

    def times(self) -> dict:
        """
        Изменение процессорного времени с момента последнего обращения
            (?) Позволяет экономить время при статистических опросах в цикле
            (!) Использующий контролирует интервал между запусками, от этого зависит релевантность результатов
        (?) Рекомендуется использовать не реже, чем раз в 1 час - чтобы статитиска успела набежать
        """
        return self._cpu_times.get()

    def used_stats(self) -> dict:
        """
        Статистика использования процессора с момента последнего обращения
        (?) Рекомендуется использовать не реже, чем раз в 1 час - чтобы статитиска успела набежать
        """
        return self._cpu_used_stats.get()

    def processes_stats(self) -> dict:
        """ Статистика по процессам, потокам, соединениям, дискрипторам """
        return self._processes_stats.get()

    def all_stats(self) -> dict:
        stats = {
            'percent': self.usage_percent(),
            'freq': self.freq()['current'],
            'times': self.times(),
            'used': self.used_stats(),

            'processes_stats': self._processes_stats.get(),
            'network': self._net_io_stats.get(),
            'disk': self._disk_io_stats.get()
        }

        return stats
