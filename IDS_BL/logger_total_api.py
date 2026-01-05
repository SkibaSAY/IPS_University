from fastapi import FastAPI, HTTPException
from datetime import datetime
from threading import Lock, Thread
from http import HTTPStatus
from loggers.machine import MachineTotalLogger

total_machine_logger = MachineTotalLogger()
lock = Lock()

app = FastAPI()
@app.get("/info", status_code=HTTPStatus.OK)
async def get_info():
    return total_machine_logger.get_info() | {'actual_date_time': datetime.now()}

@app.get("/start", status_code=HTTPStatus.OK)
async def start():
    if lock.acquire(blocking=False):
        try:
            total_machine_logger.start()
        finally:
            lock.release()
    else:
        status = total_machine_logger.get_info()['status']['name']
        return HTTPException(status_code=HTTPStatus.LOCKED,
                             detail=f'Действие невозможно: Логгер находится в состоянии: {status}')

#TODO: Добавить возможность отложенного запуска
@app.get("/stop", status_code=HTTPStatus.OK)
async def stop():
    if lock.acquire(blocking=False):
        try:
            total_machine_logger.stop()
        finally:
            lock.release()
    else:
        status = total_machine_logger.get_info()['status']['name']
        return HTTPException(status_code=HTTPStatus.LOCKED,
                             detail=f'Действие невозможно: Логгер находится в состоянии: {status}')

def initial_start():
    # блокируем на время старта возможность методов
    with lock:
        total_machine_logger.start()

# Асинхронно стартуем
initial_task = Thread(target=initial_start)
initial_task.start()

#старт через терминал:
# uvicorn logger_total_api:app --reload --port 8000