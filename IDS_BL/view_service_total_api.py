from fastapi import FastAPI
from pydantic import BaseModel
from http import HTTPStatus
from datetime import datetime
from view_services.total_manager import TotalViewManager

view_manager = TotalViewManager()
#print(view_manager.get_analyzed_items_by_date(datetime(year=2026, month=1, day=9, hour=9, minute=51)))
print(view_manager.get_learning_items_by_hour(datetime.now(), 12))
app = FastAPI()


class GetLearningBody(BaseModel):
    date_time: datetime = None
    hour: int

@app.post("/total/get_learning", status_code=HTTPStatus.OK)
async def get_learning_data(fltr: GetLearningBody):
    now = datetime.now()
    view_result = view_manager.get_learning_items_by_hour(query_date_time=fltr.date_time or now,
                                                          hour=fltr.hour)
    return  {'actual_date_time': now, 'learning_data': view_result}


class GetActualBody(BaseModel):
    date_time: datetime = None

@app.post("/total/get_actual/", status_code=HTTPStatus.OK)
async def get_analyzed_data(fltr: GetActualBody):
    now = datetime.now()
    view_result = view_manager.get_analyzed_items_by_date(date_time=fltr.date_time or now)
    return  {'actual_date_time': now, 'analyzed_data': view_result}

#команда запуска:
# uvicorn view_service_total_api:app --reload --port 8001