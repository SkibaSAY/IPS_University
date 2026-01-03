from loggers.machine.helpers import get_top_processes

#print(get_top_processes(10))
#print(get_cpu_usage())

#from loggers.machine import MachineTotalLogger
#logger = MachineTotalLogger()
#logger.start()

#input('Press key to stop ...')

import psutil
disk_usage = psutil.disk_usage('/')
print(disk_usage)

print(psutil.disk_io_counters())