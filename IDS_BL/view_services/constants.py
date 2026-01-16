from pydantic import BaseModel
from enum import StrEnum
from typing import Optional
from datetime import datetime


class ViewType(StrEnum):
    TOTAL = 'total'
    CPU = 'cpu'
    DISK = 'disk'
    NETWORK = 'network'


class ApiBaseModel(BaseModel):
    date_time: datetime = None
    """ Дата, относительно которой требуется расчитать данные """

    view_type: ViewType = None
    """ Категория данных в представлении """

    def __init__(self, date_time: Optional[datetime] = None, view_type: Optional[str] = None, **kwargs):
        if not date_time:
            date_time = datetime.now()

        if not view_type:
            view_type = ViewType.TOTAL
        else:
            view_type = ViewType(view_type)

        super().__init__(date_time=date_time, view_type=view_type, **kwargs)


class GetLearningBody(ApiBaseModel):
    hour: int = None


class GetActualBody(ApiBaseModel):
    pass