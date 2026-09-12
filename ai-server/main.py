import json
import socket
import threading

import config
import protocol
import app


def handle_client(conn, addr):
    try:
        while True:
            request_text = protocol.read_message(conn)
            if request_text is None:
                break

            try:
                request = json.loads(request_text)
                response = app.dispatch(request)
            except Exception as e:
                response = {"error": str(e)}

            response_text = json.dumps(response)
            protocol.write_message(conn, response_text)
    finally:
        conn.close()


def main():
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind((config.SERVER_HOST, config.SERVER_PORT))
    server_socket.listen(5)
    print("AI 소켓 서버 시작: " + config.SERVER_HOST + ":" + str(config.SERVER_PORT))

    while True:
        conn, addr = server_socket.accept()
        thread = threading.Thread(target=handle_client, args=(conn, addr))
        thread.daemon = True
        thread.start()


if __name__ == "__main__":
    main()
