""" Модуль для классами для network соединений """

from dataclasses import dataclass
from typing import Optional

@dataclass
class ConnectionInfo:
    """ Соединение """
    source_id: Optional[int] = None,
    source_port: Optional[int] = None,
    destination_id: Optional[int] = None,
    destination_port: Optional[int] = None
