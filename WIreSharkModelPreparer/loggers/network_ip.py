import scapy.all as scapy
# Запуск sniffer'a на дефолтном интерфейсе

def packet_callback(packet):
    if packet.haslayer(scapy.TCP):
        #print(1)
        print(packet.json())

try:
    scapy.sniff(prn=packet_callback, store=0, timeout=60) 
except Exception as e:
    print(f"Ошибка во время перехвата: {e}")