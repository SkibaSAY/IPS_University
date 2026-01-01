#pip install setuptools

import GPUtil

gpus = GPUtil.getGPUs()

if gpus:
    for gpu in gpus:
        print(f"GPU ID: {gpu.id}")
        print(f"Название: {gpu.name}")
        print(f"Загрузка: {gpu.load*100:.2f}%")
        print(f"Использование памяти: {gpu.memoryUtil*100:.2f}%")
        print(f"Температура: {gpu.temperature:.2f}°C")
        print("-" * 20)
else:
    print("GPU не обнаружено.")