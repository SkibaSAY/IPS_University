"""
Расширенный базовый планировщик задач
"""

from sched import scheduler
from time import time
from datetime import datetime
from threading import Thread


#TODO: рассмотреть переход на более совершенную библиотеку - пока мне важна простота
class SchedulerEx(scheduler):
    _scheduler_task = None

    def run_async(self):
        """ Старт без блокировки активного процесса """
        # Запускаем планировщик в отдельном потоке
        self._scheduler_task = Thread(target=self.run, kwargs={'blocking': True})
        self._scheduler_task.start()

    def repeat(self, delay, interval, priority, action, *args, **kwargs):
        def repeat_action():
            start_time = datetime.now()
            action(*args, **kwargs)
            finish_time = datetime.now()

            duration = (finish_time - start_time).total_seconds()
            next_delay = interval - duration if interval > duration else 0
            self.enter(next_delay, priority, repeat_action, args, kwargs)

        self.enter(delay, priority, repeat_action, args, kwargs)
