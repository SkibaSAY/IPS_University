"""
Docstring for helpers.absolute_stat
"""

class AbsoluteStat:
    """
    Некоторые характеристики только растут со временем, накапливаясь от старта системы
    (!) Класс позволяет работать с таким характеристиками, вычисляя прирост с последнего обращения
    (?) Соблюдение частоты обращения должно контролироваться разработчиков - иначе получите неравнозначные данные
        (за разные интервалы времени)
    """
    _old_value = None
    _get_func = None

    def __init__(self, get_func):
        self._get_func = get_func

    def _get_new_value(self, *args, **kwargs):
        if self._old_value is None:
            self._old_value = self._get_func(*args, **kwargs)
            return self._old_value

        return self._get_func(*args, **kwargs)

    def _compute_result_value(self, new_value):
        # Для int значений актуально, вычисляет прирост
        return new_value - self._old_value

    def get(self, *args, **kwargs):
        new_value = self._get_new_value(*args, **kwargs)
        result = self._compute_result_value(new_value)
        self._old_value = new_value
        return result


class AbsoluteDictStat(AbsoluteStat):
    _compute_fields: list[str] = None
    def __init__(self, get_func, compute_fields: list[str] = None):
        self._compute_fields = compute_fields
        super().__init__(get_func)

    def _compute_result_value(self, new_value):
        fields = self._compute_fields or list(new_value.keys())
        return {f:getattr(new_value, f) - getattr(self._old_value,f) for f in fields}
