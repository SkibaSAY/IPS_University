#from loggers.machine.helpers import get_top_processes, get_disk_usage

#print(get_top_processes(10))
#print(get_cpu_usage())

#from loggers.machine import MachineTotalLogger
#logger = MachineTotalLogger()
#logger.start()

#input('Press key to stop ...')

# from loggers.machine.stat_managers import CPUStatManager
# from time import sleep
# cpu = CPUStatManager()

# print(cpu.all_stats())
# sleep(10)
# print(cpu.all_stats())

# from pymongo import MongoClient
# mongo_client = MongoClient("mongodb://localhost:27017/")
# db = mongo_client['test_ids']

# cursor = db['time_09_50'].find({}, {'_id':0})
# for file in cursor:
#     print(file)

