import time
from PIL import ImageGrab
from PIL import Image
import socket

udp_host = "192.168.1.6"
udp_port = 12347
sock = socket.socket(family=socket.AF_INET, type=socket.SOCK_DGRAM)


def screenSync():
    import ctypes
    timeLast = time.time()
    user32 = ctypes.windll.user32
    screensize = (user32.GetSystemMetrics(0), user32.GetSystemMetrics(1))
    leftBBox = (screensize[0]/4, screensize[1]/4*3, screensize[0]/4*3, screensize[1])
    rightBBox = (screensize[0]/2, 0, screensize[0], screensize[1])
    print(socket.gethostname())

    try:
        while 1 < 2:
            if (time.time() - timeLast) > 0.001:
                leftImg = ImageGrab.grab(bbox = leftBBox)
                rightImg = ImageGrab.grab(bbox = rightBBox)
                leftAvg = leftImg.resize((1, 1))
                leftColor = leftAvg.getpixel((0,0))
                rightAvg = rightImg.resize((1,1))
                rightColor = rightAvg.getpixel((0,0))

                stringArr = [str(leftColor[0]), str(leftColor[1]), str(leftColor[2])]

                (r,g,b) = leftColor
                lByte = bytes((1,r,g,b))
                sock.sendto(lByte, (udp_host,udp_port))
                print(lByte)
    except KeyboardInterrupt:
        print("Loop Ended")
        code = bytes((99,255,255,255))
        sock.sendto(code, (udp_host,udp_port))

def setColor(r,g,b):
    vector = (0,r,g,b,0,0,0)
    colorByte = bytes(vector)
    sock.sendto(colorByte, (udp_host,udp_port))
    
def rainbow():
    vector = (2,0,0,0,0,0,0)
    codeByte = bytes(vector)
    sock.sendto(codeByte, (udp_host,udp_port))
    
def colorWipe(r,g,b,r2,g2,b2):
    vector = (3,r,g,b,r2,g2,b2)
    codeByte = bytes(vector)
    sock.sendto(codeByte, (udp_host,udp_port))
