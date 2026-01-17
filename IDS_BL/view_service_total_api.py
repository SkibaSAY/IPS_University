from fastapi import FastAPI
from http import HTTPStatus
from datetime import datetime
from view_services.total_manager import TotalViewManager
from view_services.constants import GetActualBody, GetLearningBody, ViewType

view_manager = TotalViewManager()
#print(view_manager.get_analyzed_items_by_date(datetime(year=2026, month=1, day=9, hour=9, minute=51), ViewType.CPU))
#print(view_manager.get_analyzed_items_by_date(datetime(year=2026, month=1, day=9, hour=9, minute=51), ViewType.DISK))
#print(view_manager.get_learning_items_by_hour(datetime(year=2026, month=1, day=9, hour=9, minute=51), 12, ViewType.DISK))
app = FastAPI()


@app.post("/get_learning", status_code=HTTPStatus.OK)
async def get_learning_data(body: GetLearningBody):
    cur_date = datetime.now()
    view_result = view_manager.get_learning_items_by_hour(query_date_time=body.date_time,
                                                          hour=body.hour,
                                                          view_type=body.view_type)
    return  {'learning_data': view_result,
             'current_date_time': cur_date
            }


@app.post("/get_actual/", status_code=HTTPStatus.OK)
async def get_actual_data(body: GetActualBody):
    cur_date = datetime.now()
    view_result = view_manager.get_analyzed_items_by_date(date_time=body.date_time, view_type=body.view_type)
    return  {'actual_date_time': cur_date, 'last_items': view_result}


#команда запуска:
# uvicorn view_service_total_api:app --reload --port 8001
