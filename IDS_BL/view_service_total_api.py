from fastapi import FastAPI
from http import HTTPStatus
from datetime import datetime
from view_services.total_manager import TotalViewManager

view_manager = TotalViewManager()

app = FastAPI()
@app.post("/total/get_learning", status_code=HTTPStatus.OK)
async def get_learning_data(fltr: dict):
    now = datetime.now()
    view_result = view_manager.get_learning_items_by_hour(query_date_time=fltr.get('date_time') or now,
                                                          hour=fltr['hour'])
    return  {'actual_date_time': now, 'learning_data': view_result}

@app.post("/total/get_actual", status_code=HTTPStatus.OK)
async def get_analyzed_data(fltr: dict):
    now = datetime.now()
    view_result = view_manager.get_analyzed_items_by_date(query_date_time=fltr.get('date_time') or now)
    return  {'actual_date_time': now, 'analyzed_data': view_result}

#команда запуска:
# uvicorn view_service_total_api:app --reload --port 8001