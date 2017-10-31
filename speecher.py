# python 3.5

import sys
import speech_recognition as sr
import threading
import time
import queue
import socket
import copy
import serial
import math
from time import sleep
import pyaudio
import wave
import numpy as np

ADD_TH = 1500
MIN_TH = 2000
PORT = 8000
MAX_RECV_LEN = 32
DEFAULT_NUM = 0
SILENT_TIME = 0.5
TIMEOUT = 100000
HERTZ = 440
chunk = 2048
flag = 0

recog = sr.Recognizer()
mics = sr.Microphone.list_microphone_names()
mic = None
queue = queue.Queue()

def sendSpeech(server_socket):
    # get speech from queue and send it
    while True:
        if not queue.empty():
            str = queue.get()
            sys.stdout.flush()
            server_socket.send(str.encode('utf-8'))
            print('[send message]: %s'%(str))
    server_socket.close()

#port name
# s=serial.Serial("/dev/cu.usbmodem1421",9600)

def recognize(audio, timeID):
    # recognize speech using Google Speech Recognition'
    global flag
    try:
        value = recog.recognize_google(audio, language = "en_us")

        # we need some special handling here to correctly print unicode characters to standard output
        if str is bytes:  # this version of Python uses bytes for strings (Python 2)
            print("You said {0} at {1:.3f}".format(value, timeID).encode("utf-8"))
            # queue.put(value.encode('utf-8'))
        else:  # this version of Python uses unicode for strings (Python 3+)
            print("You said {0} at {1:.3f}".format(value, timeID))
            # queue.put(value)
        if 'pikachu' in value:
            print (flag)
            # s.write(str(flag).encode())
            print ("Send H to Arduino")
            queue.put(str(flag))
            sleep(10)
            # s.write('0'.encode())
            queue.put('0')
            print ("Send L to Arduino")
            flag = 0
    except sr.UnknownValueError:
        print("Oops! Didn't catch that at {0:.3f}".format(timeID))
        return
    except sr.RequestError as e:
        print("Uh oh! Couldn't request results from Google Speech Recognition service at {0:.3f}; {1}".format(timeID, e))
        return

# def Detect_Frequency():
#     # open up a wave
#     wf = wave.open('microphone-results.wav', 'rb')
#     swidth = wf.getsampwidth()
#     RATE = wf.getframerate()
#     window = np.blackman(chunk)
#     p = pyaudio.PyAudio()
#     stream = p.open(format =
#                     p.get_format_from_width(wf.getsampwidth()),
#                     channels = wf.getnchannels(),
#                     rate = RATE,
#                     output = True)
#     data = wf.readframes(chunk)
#     count = 0
#     while len(data) == chunk*swidth:
#         # write data out to the audio stream
#         stream.write(data)
#         # unpack the data and times by the hamming window
#         indata = np.array(wave.struct.unpack("%dh"%(len(data)/swidth),\
#                                              data))*window
#         # Take the fft and square each value
#         fftData=abs(np.fft.rfft(indata))**2
#         # find the maximum
#         which = fftData[1:].argmax() + 1
#         # use quadratic interpolation around the max
#         if which != len(fftData)-1:
#             y0,y1,y2 = np.log(fftData[which-1:which+2:])
#             x1 = (y2 - y0) * .5 / (2 * y1 - y2 - y0)
#             # find the frequency and output it
#             thefreq = (which+x1)*RATE/chunk
#             print ("The freq is %f Hz." % (thefreq))
#             if thefreq > 800:
#                 count += 1
#         else:
#             thefreq = which*RATE/chunk
#             print ("The freq is %f Hz." % (thefreq))
#             if thefreq > 800:
#                 count += 1
#         if count == 5:
#             return True
#         # read some more data
#         data = wf.readframes(chunk)
#     stream.close()
#     p.terminate()
#     return False

def calVolumeDB():
    wf = wave.open('microphone-results.wav', 'rb')
    p = wf.getparams()
    f = p[3]
    s = wf.readframes(f)
    print(f)
    wf.close()
    s = np.fromstring(s, np.int16)
    if f < 100000:
        return 0
    s_index = (s > 2000)
    s = s[s_index]
    s = np.sum(s) / len(s)
    return s

if __name__ == '__main__':

    # confirm arg number
    # if len(sys.argv) != 2:
    #     print("[ers error]")
    #     exit()

    # find microphone by name
    index = -1
    for idx, name in enumerate(mics):
        if name.find('USB Audio Device') != -1:
            index = idx
            break
    if index == -1:
        print('[microphone not found]')
        exit()
    mic = sr.Microphone(device_index = index)

    # init microphone
    print("A moment of silence, please...")
    with mic as source: recog.adjust_for_ambient_noise(source)
    print("Origin threshold: {0}".format(recog.energy_threshold))
    recog.energy_threshold = max(recog.energy_threshold + ADD_TH, MIN_TH)
    recog.dynamic_energy_threshold = False
    print("Set minimum energy threshold to {} \n".format(recog.energy_threshold))
    recog.pause_threshold = SILENT_TIME

    # init socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    # connect to receiver
    receiver_ip = sys.argv[1]
    connected = False
    while not connected:
        try:
            sock.connect( (receiver_ip, PORT) )
            connected = True
        except socket.error:
            msg = sys.exc_info()[0]
            print('[connect fail]: \t%s'%(msg))
            time.sleep(1)

    print('[connect success]')
    print('IP: %s'%(sock.getpeername()[0]))

    # send identification message
    sock.send('speecher'.encode('utf-8'))

    # open queue, ready for sending speech
    th = threading.Thread(target = sendSpeech, args = (sock, ));
    th.daemon = True
    th.start()

    print('Waiting server to register....')
    ok_message = sock.recv(MAX_RECV_LEN).decode('utf-8')
    if ok_message == 'ok':
        print('[register success]')
    else:
        print('[register error]')
        exit()

    input('Press enter to start recognizing: ')

    try:
        while True:
            # receive audio
            print("Say something!")
            with mic as source: audio = recog.listen(source, phrase_time_limit  = TIMEOUT)
            with open("microphone-results.wav", "wb") as f:
                f.write(audio.get_wav_data())
            print("Got it! Now to recognize it...")
            # create Process to recognize in multithread
            volume = calVolumeDB()
            print (volume)
            if volume > 20000:
                flag = 3
                copy_audio = copy.copy(audio)
                th = threading.Thread(target = recognize, args = (copy_audio, time.clock(), ));
                th.daemon = True
                th.start()
            elif volume > 15000:
                flag = 2
                copy_audio = copy.copy(audio)
                th = threading.Thread(target = recognize, args = (copy_audio, time.clock(), ));
                th.daemon = True
                th.start()
            elif volume > 8000:
                flag = 1
                copy_audio = copy.copy(audio)
                th = threading.Thread(target = recognize, args = (copy_audio, time.clock(), ));
                th.daemon = True
                th.start()

    except KeyboardInterrupt:
        pass
