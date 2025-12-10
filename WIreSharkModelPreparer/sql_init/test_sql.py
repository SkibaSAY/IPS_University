#pip install psycopg2

import psycopg2

connection = psycopg2.connect(dbname="IDS_SAY", host="127.0.0.1", port="5432", 
                              user="event_collector", password="password")

print("Подключение установлено")

connection.close()
