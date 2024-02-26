#!/usr/bin/env python

import asyncio
import datetime
import random
import websockets
import time

CONNECTIONS = set()

async def register(websocket):
    print('new client')
    CONNECTIONS.add(websocket)
    try:
        await websocket.wait_closed()
    finally:
        CONNECTIONS.remove(websocket)

async def show_time():
    while True:
        message = datetime.datetime.utcnow().isoformat() + "Z"
        websockets.broadcast(CONNECTIONS, message)
        await asyncio.sleep(random.random() * 2 + 1)

async def handle_input():
    while True:
        await asyncio.sleep(0.01)
        message = input()
        message = '{{"type":"playerLocation","x":0,"y":0,"direction":{0},"timestamp":{1}}}'.format(message, time.time() * 1000)
        print(message)
        websockets.broadcast(CONNECTIONS, message)

async def main():
    async with websockets.serve(register, "localhost", 8765):
        await handle_input()

if __name__ == "__main__":
    asyncio.run(main())
