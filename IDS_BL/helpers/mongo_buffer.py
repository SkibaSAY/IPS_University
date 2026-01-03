"""
https://www.w3schools.com/python/python_mongodb_insert.asp
"""

from pymongo import MongoClient
from threading import Lock
from .scheduler_ex import SchedulerEx


__author__ = 'Сластухин А.Ю.'


class MongoBuffer:
    """ Буффер для отложенной отправки в монгу """
    _buffer: dict = None
    _buffer_lock: Lock = None
    _send_interval = 1 * 60
    _scheduler = None

    _client: MongoClient = None
    _db = None

    def __init__(self, connection: str = 'mongodb://localhost:27017/', db_name: str = 'test_ids'):
        self._buffer = {}
        self._buffer_lock = Lock()

        self._client = MongoClient(connection)
        self._db = self._client[db_name]
        self._start_repeat()

    def _start_repeat(self):
        """ Настраиваем повторение """
        self._scheduler = SchedulerEx()
        self._scheduler.repeat(0, self._send_interval, 1, self._send_all)
        self._scheduler.run_async()

    def __del__(self):
        """ Деструктор: выполняется при завершении работы """
        self._send_all()

    def _send_all(self):
        with self._buffer_lock:
            while self._buffer:
                collection, files = self._buffer.popitem()
                db_collection = self._db[collection]
                db_collection.insert_many(files)

    def add(self, collection: str, file: dict) -> None:
        with self._buffer_lock:
            current_collection_list: list = self._buffer.setdefault(collection, [])
            current_collection_list.append(file)
