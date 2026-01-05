"""
Расширенный базовый планировщик задач
"""

from sched import scheduler
from time import time, sleep
from datetime import datetime
from threading import Thread, Event

#TODO: рассмотреть переход на более совершенную библиотеку - пока мне важна простота
class SchedulerEx(scheduler):
    _scheduler_task = None
    _cancel_token = None
    _name = None

    def __init__(self, name, time_func = None, sleep_func = None, cancel_token = None):
        self._cancel_token = cancel_token or Event()
        self._name = name
        super().__init__(time_func or time, sleep_func or sleep)

    def run_async(self):
        """ Старт без блокировки активного процесса """
        # Запускаем планировщик в отдельном потоке
        self.cancel_token = Event()
        self._scheduler_task = Thread(target=self.run, kwargs={'blocking': True})
        #TODO: в случае ошибки потока - приложение не падает, подумать над пробросом исключения выше
        self._scheduler_task.start()

    def log(self, msg):
        alias = f'SchedulerEx("{self._name}")'
        print(f'{alias}: {msg}')

    def stop(self):
        if self.cancel_token:
            if self.cancel_token.is_set():
                print('Получено событие остановки - останавливаюсь')
            else:
                print('Инициирую остановку')
                self.cancel_token.set()

            # ждём остановки
            self._scheduler_task.join()

        # подчищаем очередь выполнения
        self.cancel()

    def repeat(self, delay, interval, priority, action, *args, **kwargs):
        def repeat_action():
            try:
                if self.cancel_token and self.cancel_token.is_set():
                    return

                start_time = datetime.now()
                action(*args, **kwargs)
                finish_time = datetime.now()

                duration = (finish_time - start_time).total_seconds()
                next_delay = interval - duration if interval > duration else 0
                self.log(f'start: {start_time}, finish: {finish_time}, next_delay: {next_delay}')
                self.enter(next_delay, priority, repeat_action, args, kwargs)
            except:
                self.stop()

        self.enter(delay, priority, repeat_action)
