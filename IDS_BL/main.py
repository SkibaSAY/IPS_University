from loggers.machine.helpers import get_top_processes, get_cpu_usage, get_cpu_times

#print(get_top_processes(10))
#print(get_cpu_usage())

# from loggers.machine import MachineTotalLogger
# logger = MachineTotalLogger()
# logger.run()

# input('Press key to stop ...')
import pymongo
myclient = pymongo.MongoClient("mongodb://localhost:27017/")

mydb = myclient["mydatabase"]

print(mydb)