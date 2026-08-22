from __future__ import annotations

import re
import threading
from dataclasses import dataclass

import serial
from serial import SerialException
from serial.tools import list_ports


class STM32DeviceError(Exception):
    """Raised when communication with the STM32 device fails."""


@dataclass(frozen=True)
class DeviceInfo:
    device: str
    board: str
    firmware: str


@dataclass(frozen=True)
class BME280Data:
    chip_id: int
    temperature: float
    humidity: float
    pressure: float


class STM32Device:
    """Simple Python interface for the STM32 UART firmware."""

    DEFAULT_BAUDRATE = 115200
    DEFAULT_TIMEOUT = 1.0

    INFO_PATTERN = re.compile(
        r"^OK\s+DEVICE=\s*(?P<device>\S+)\s+"
        r"BOARD=\s*(?P<board>\S+)\s+"
        r"FW=\s*(?P<fw>\S+)\s*$"
    )

    BME280_PATTERN = re.compile(
        r"^OK\s+BME280\s+"
        r"ID=0x(?P<id>[0-9A-Fa-f]{2})\s+"
        r"TEMP=(?P<temp>-?\d+(?:\.\d+)?)C\s+"
        r"HUM=(?P<hum>\d+(?:\.\d+)?)%\s+"
        r"PRESS=(?P<press>\d+(?:\.\d+)?)hPa\s*$",
        re.IGNORECASE,
    )

    def __init__(
        self,
        port: str,
        baudrate: int = DEFAULT_BAUDRATE,
        timeout: float = DEFAULT_TIMEOUT,
    ) -> None:
        self.port = port
        self.baudrate = baudrate
        self.timeout = timeout
        self.serial_port = None
        self.serial_lock = threading.Lock()

        self.open()

    @staticmethod
    def available_ports() -> list[str]:
        return [port.device for port in list_ports.comports()]

    @property
    def is_connected(self) -> bool:
        return self.serial_port is not None and self.serial_port.is_open

    def open(self) -> None:
        try:
            self.serial_port = serial.Serial(
                port=self.port,
                baudrate=self.baudrate,
                timeout=self.timeout,
                write_timeout=self.timeout,
            )
        except SerialException as error:
            raise STM32DeviceError(
                f"Could not open {self.port}: {error}"
            ) from error

        self.serial_port.reset_input_buffer()
        self.serial_port.reset_output_buffer()

    def close(self) -> None:
        with self.serial_lock:
            if self.serial_port is not None and self.serial_port.is_open:
                self.serial_port.close()

    def send_command(self, command: str) -> str:
        command = command.strip()

        if not command:
            raise STM32DeviceError("Command cannot be empty.")

        if not self.is_connected:
            raise STM32DeviceError("Device is not connected.")

        with self.serial_lock:
            try:
                self.serial_port.reset_input_buffer()
                self.serial_port.write(f"{command}\n".encode("ascii"))
                self.serial_port.flush()
                raw_response = self.serial_port.readline()

            except (SerialException, OSError) as error:
                raise STM32DeviceError(
                    f"Serial communication failed: {error}"
                ) from error

        if not raw_response:
            raise STM32DeviceError(
                f"Timeout waiting for response to '{command}'."
            )

        try:
            response = raw_response.decode("ascii").strip()
        except UnicodeDecodeError as error:
            raise STM32DeviceError(
                "Device returned non-ASCII data."
            ) from error

        if response.startswith("ERR"):
            raise STM32DeviceError(response)

        return response

    def test(self) -> bool:
        return self.send_command("TEST") == "OK TEST"

    def get_info(self) -> DeviceInfo:
        response = self.send_command("GET INFO")
        match = self.INFO_PATTERN.match(response)

        if match is None:
            raise STM32DeviceError(
                f"Unexpected GET INFO response: {response}"
            )

        return DeviceInfo(
            device=match.group("device"),
            board=match.group("board"),
            firmware=match.group("fw"),
        )

    def get_bme280(self) -> BME280Data:
        response = self.send_command("GET BME280")
        match = self.BME280_PATTERN.match(response)

        if match is None:
            raise STM32DeviceError(
                f"Unexpected GET BME280 response: {response}"
            )

        return BME280Data(
            chip_id=int(match.group("id"), 16),
            temperature=float(match.group("temp")),
            humidity=float(match.group("hum")),
            pressure=float(match.group("press")),
        )

    def set_led(self, state: bool) -> str:
        if state:
            return self.send_command("LED 1")

        return self.send_command("LED 0")

    def get_led(self) -> str:
        return self.send_command("GET LED")

    def get_button(self) -> str:
        return self.send_command("GET BUTTON")
