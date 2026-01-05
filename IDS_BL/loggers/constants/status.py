from enum import Enum


class WorkingStatus(Enum):
    UNKNOWN = 0
    STARTING = 1
    WORKING = 2
    WAITING = 3
    STOPING = 4
    STOPED = 5
    HAS_ERROR = 6
