import struct

# 메시지 형식: [4바이트 길이(빅엔디안)][그 길이만큼의 UTF-8 텍스트(JSON)]


def recv_exact(sock, size):
    """정확히 size 바이트를 받을 때까지 반복해서 읽는다. 연결이 끊기면 None을 반환한다."""
    chunks = []
    remaining = size
    while remaining > 0:
        chunk = sock.recv(remaining)
        if not chunk:
            return None
        chunks.append(chunk)
        remaining -= len(chunk)
    return b"".join(chunks)


def read_message(sock):
    """소켓에서 길이-접두 메시지 하나를 읽어 문자열로 반환한다. 연결 종료 시 None."""
    length_bytes = recv_exact(sock, 4)
    if length_bytes is None:
        return None

    length = struct.unpack(">I", length_bytes)[0]
    body_bytes = recv_exact(sock, length)
    if body_bytes is None:
        return None

    return body_bytes.decode("utf-8")


def write_message(sock, text):
    """문자열을 길이-접두 메시지로 소켓에 보낸다."""
    body_bytes = text.encode("utf-8")
    header_bytes = struct.pack(">I", len(body_bytes))
    sock.sendall(header_bytes)
    sock.sendall(body_bytes)
