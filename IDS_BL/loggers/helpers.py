from datetime import datetime


def get_target_collection(date_time: datetime) -> str:
    """ Округляем до 10 мин """
    minutes_floor = date_time.minute // 10 * 10
    return f'time_{date_time.replace(minute=minutes_floor).strftime("%H_%M")}'