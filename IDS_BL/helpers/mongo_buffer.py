"""
https://www.w3schools.com/python/python_mongodb_insert.asp
"""

from pymongo import MongoClient
from datetime import datetime
from threading import Timer


__author__ = 'Сластухин А.Ю.'


class MongoBuffer:
    """ Буффер для отложенной отправки в монгу """
    _is_dispose: bool = False

    _buffer: dict = None
    _last_send_time = None
    _send_interval = 1 * 60
    _timer = None

    _client: MongoClient = None
    _db = None

    def __init__(self, connection: str = 'mongodb://localhost:27017/', db_name: str = 'test_ids'):
        self._buffer = {}
        self._last_send_time = datetime.now()

        self._client = MongoClient(connection)
        self._db = self._client[db_name]

    def __del__(self):
        """ Деструктор: выполняется при завершении работы """
        self._is_dispose = True
        self._send_all

    def _reset_timer(self):
        self._timer = Timer(interval=self._send_interval, function=self._send_all)

    def _send_all(self):
        for collection, files in self._buffer:
            db_collection = self._db[collection]
            db_collection.insert_many(files)

        self._last_send_time = datetime.now()
        if not self._is_dispose:
            self._reset_timer()

    def add(self, collection: str, file: dict) -> None:
        current_collection_list: list = self._buffer.setdefault(collection, [])
        current_collection_list.append(file)
