from __future__ import annotations

import queue
import threading
import tkinter as tk
from tkinter import messagebox, ttk

from device import BME280Data, STM32Device, STM32DeviceError


SAMPLE_PERIOD_MS = 1000


class EnvironmentMonitorApp:
    def __init__(self, root: tk.Tk) -> None:
        self.root = root
        self.root.title("STM32U575 Environment Monitor")
        self.root.geometry("820x620")
        self.root.minsize(720, 560)

        self.device = None
        self.led_on = False
        self.messages = queue.Queue()
        self.poll_job = None
        self.poll_in_progress = False

        self.port_var = tk.StringVar()
        self.status_var = tk.StringVar(value="Disconnected")

        self.device_name_var = tk.StringVar(value="—")
        self.board_var = tk.StringVar(value="—")
        self.firmware_var = tk.StringVar(value="—")

        self.temperature_var = tk.StringVar(value="— °C")
        self.humidity_var = tk.StringVar(value="— %")
        self.pressure_var = tk.StringVar(value="— hPa")

        self.command_var = tk.StringVar()
        self.response_var = tk.StringVar(value="No command sent yet.")

        self.build_ui()
        self.refresh_ports()

        self.root.after(100, self.process_messages)
        self.root.protocol("WM_DELETE_WINDOW", self.close_application)

    def build_ui(self) -> None:
        container = ttk.Frame(self.root, padding=16)
        container.pack(fill="both", expand=True)

        connection_frame = ttk.LabelFrame(
            container,
            text="Connection",
            padding=12,
        )
        connection_frame.pack(fill="x")

        ttk.Label(connection_frame, text="COM port").grid(
            row=0,
            column=0,
            sticky="w",
            padx=(0, 8),
        )

        self.port_combo = ttk.Combobox(
            connection_frame,
            textvariable=self.port_var,
            state="readonly",
            width=18,
        )
        self.port_combo.grid(row=0, column=1, sticky="w")

        ttk.Button(
            connection_frame,
            text="Refresh",
            command=self.refresh_ports,
        ).grid(row=0, column=2, padx=8)

        self.connect_button = ttk.Button(
            connection_frame,
            text="Connect",
            command=self.toggle_connection,
        )
        self.connect_button.grid(row=0, column=3, padx=(0, 16))

        ttk.Label(connection_frame, text="Status:").grid(
            row=0,
            column=4,
            sticky="e",
        )

        ttk.Label(
            connection_frame,
            textvariable=self.status_var,
        ).grid(row=0, column=5, sticky="w", padx=(6, 0))

        device_frame = ttk.LabelFrame(
            container,
            text="Device Information",
            padding=12,
        )
        device_frame.pack(fill="x", pady=(12, 0))

        self.add_info_item(
            device_frame,
            "Device",
            self.device_name_var,
            0,
        )
        self.add_info_item(
            device_frame,
            "Board",
            self.board_var,
            1,
        )
        self.add_info_item(
            device_frame,
            "Firmware",
            self.firmware_var,
            2,
        )

        sensor_frame = ttk.Frame(container)
        sensor_frame.pack(fill="x", pady=14)

        self.add_sensor_value(
            sensor_frame,
            "Temperature",
            self.temperature_var,
            0,
        )
        self.add_sensor_value(
            sensor_frame,
            "Humidity",
            self.humidity_var,
            1,
        )
        self.add_sensor_value(
            sensor_frame,
            "Air Pressure",
            self.pressure_var,
            2,
        )

        control_frame = ttk.LabelFrame(
            container,
            text="Device Controls",
            padding=12,
        )
        control_frame.pack(fill="x")

        self.info_button = ttk.Button(
            control_frame,
            text="GET INFO",
            command=self.request_info,
            state="disabled",
        )
        self.info_button.grid(row=0, column=0, padx=(0, 8))

        self.led_button = ttk.Button(
            control_frame,
            text="LED ON",
            command=self.toggle_led,
            state="disabled",
        )
        self.led_button.grid(row=0, column=1)

        command_frame = ttk.LabelFrame(
            container,
            text="Send Message / Command",
            padding=12,
        )
        command_frame.pack(fill="x", pady=(14, 0))

        self.command_entry = ttk.Entry(
            command_frame,
            textvariable=self.command_var,
        )
        self.command_entry.grid(
            row=0,
            column=0,
            sticky="ew",
            padx=(0, 8),
        )
        self.command_entry.bind("<Return>", self.enter_pressed)

        self.send_button = ttk.Button(
            command_frame,
            text="Send",
            command=self.send_custom_command,
            state="disabled",
        )
        self.send_button.grid(row=0, column=1)

        command_frame.columnconfigure(0, weight=1)

        response_frame = ttk.LabelFrame(
            container,
            text="Device Response",
            padding=12,
        )
        response_frame.pack(fill="both", expand=True, pady=(14, 0))

        self.response_label = tk.Label(
            response_frame,
            textvariable=self.response_var,
            anchor="nw",
            justify="left",
            font=("Consolas", 10),
            wraplength=740,
        )
        self.response_label.pack(fill="both", expand=True)

    def add_info_item(
        self,
        parent,
        label: str,
        variable: tk.StringVar,
        column: int,
    ) -> None:
        item_frame = ttk.Frame(parent)
        item_frame.grid(
            row=0,
            column=column,
            sticky="w",
            padx=(0, 40),
        )

        ttk.Label(item_frame, text=label).pack(anchor="w")

        ttk.Label(
            item_frame,
            textvariable=variable,
            font=("Segoe UI", 11, "bold"),
        ).pack(anchor="w")

    def add_sensor_value(
        self,
        parent,
        label: str,
        variable: tk.StringVar,
        column: int,
    ) -> None:
        value_frame = ttk.LabelFrame(
            parent,
            text=label,
            padding=18,
        )
        value_frame.grid(
            row=0,
            column=column,
            sticky="nsew",
            padx=(0 if column == 0 else 6, 6 if column < 2 else 0),
        )

        ttk.Label(
            value_frame,
            textvariable=variable,
            font=("Segoe UI", 20, "bold"),
        ).pack(anchor="center", pady=8)

        parent.columnconfigure(column, weight=1)

    def refresh_ports(self) -> None:
        ports = STM32Device.available_ports()
        self.port_combo["values"] = ports

        if ports:
            if self.port_var.get() not in ports:
                self.port_var.set(ports[0])
        else:
            self.port_var.set("")

    def toggle_connection(self) -> None:
        if self.device is None:
            self.connect_device()
        else:
            self.disconnect_device()

    def connect_device(self) -> None:
        port = self.port_var.get().strip()

        if not port:
            messagebox.showwarning(
                "No COM port",
                "Select a COM port before connecting.",
            )
            return

        try:
            device = STM32Device(port)

            if not device.test():
                device.close()
                raise STM32DeviceError(
                    "The selected port did not respond to TEST."
                )

            info = device.get_info()

        except STM32DeviceError as error:
            messagebox.showerror(
                "Connection failed",
                str(error),
            )
            return

        self.device = device
        self.status_var.set(f"Connected to {port}")

        self.device_name_var.set(info.device)
        self.board_var.set(info.board)
        self.firmware_var.set(info.firmware)

        self.connect_button.configure(text="Disconnect")
        self.port_combo.configure(state="disabled")
        self.info_button.configure(state="normal")
        self.led_button.configure(state="normal")
        self.send_button.configure(state="normal")

        self.response_var.set("Connected successfully.")
        self.command_entry.focus_set()

        self.schedule_sensor_read(immediate=True)

    def disconnect_device(self) -> None:
        if self.poll_job is not None:
            self.root.after_cancel(self.poll_job)
            self.poll_job = None

        if self.device is not None:
            self.device.close()

        self.device = None
        self.poll_in_progress = False

        self.status_var.set("Disconnected")
        self.connect_button.configure(text="Connect")
        self.port_combo.configure(state="readonly")

        self.info_button.configure(state="disabled")
        self.led_button.configure(state="disabled")
        self.send_button.configure(state="disabled")

        self.device_name_var.set("—")
        self.board_var.set("—")
        self.firmware_var.set("—")

        self.temperature_var.set("— °C")
        self.humidity_var.set("— %")
        self.pressure_var.set("— hPa")

        self.led_on = False
        self.led_button.configure(text="LED ON")

    def schedule_sensor_read(self, immediate: bool = False) -> None:
        if self.device is None:
            return

        delay = 0 if immediate else SAMPLE_PERIOD_MS
        self.poll_job = self.root.after(
            delay,
            self.start_sensor_read,
        )

    def start_sensor_read(self) -> None:
        self.poll_job = None

        if self.device is None or self.poll_in_progress:
            return

        self.poll_in_progress = True

        threading.Thread(
            target=self.sensor_worker,
            daemon=True,
        ).start()

    def sensor_worker(self) -> None:
        try:
            if self.device is None:
                return

            sensor_data = self.device.get_bme280()
            self.messages.put(("sensor", sensor_data))

        except STM32DeviceError as error:
            self.messages.put(("error", str(error)))

        finally:
            self.messages.put(("poll_complete", None))

    def request_info(self) -> None:
        self.run_in_background(
            "info",
            self.read_info,
        )

    def read_info(self):
        if self.device is None:
            raise STM32DeviceError("Device is not connected.")

        return self.device.get_info()

    def toggle_led(self) -> None:
        new_state = not self.led_on

        self.run_in_background(
            "led",
            lambda: self.set_led(new_state),
        )

    def set_led(self, state: bool):
        if self.device is None:
            raise STM32DeviceError("Device is not connected.")

        response = self.device.set_led(state)
        return state, response

    def send_custom_command(self) -> None:
        command = self.command_var.get().strip()

        if not command:
            return

        self.run_in_background(
            "custom",
            lambda: self.send_message(command),
        )

    def send_message(self, command: str) -> str:
        if self.device is None:
            raise STM32DeviceError("Device is not connected.")

        return self.device.send_command(command)

    def run_in_background(self, message_type: str, function) -> None:
        def worker() -> None:
            try:
                result = function()
                self.messages.put((message_type, result))

            except STM32DeviceError as error:
                self.messages.put(("error", str(error)))

        threading.Thread(
            target=worker,
            daemon=True,
        ).start()

    def process_messages(self) -> None:
        try:
            while True:
                message_type, value = self.messages.get_nowait()

                if message_type == "sensor":
                    self.update_sensor_values(value)

                elif message_type == "poll_complete":
                    self.poll_in_progress = False
                    self.schedule_sensor_read()

                elif message_type == "info":
                    self.update_device_info(value)

                elif message_type == "led":
                    self.update_led_state(value)

                elif message_type == "custom":
                    self.response_var.set(str(value))

                elif message_type == "error":
                    self.response_var.set(f"ERROR: {value}")

        except queue.Empty:
            pass

        self.root.after(100, self.process_messages)

    def update_sensor_values(self, data) -> None:
        if not isinstance(data, BME280Data):
            return

        self.temperature_var.set(f"{data.temperature:.2f} °C")
        self.humidity_var.set(f"{data.humidity:.2f} %")
        self.pressure_var.set(f"{data.pressure:.2f} hPa")

    def update_device_info(self, info) -> None:
        self.device_name_var.set(info.device)
        self.board_var.set(info.board)
        self.firmware_var.set(info.firmware)

        self.response_var.set(
            f"DEVICE={info.device}  "
            f"BOARD={info.board}  "
            f"FW={info.firmware}"
        )

    def update_led_state(self, result) -> None:
        state, response = result

        self.led_on = state

        if self.led_on:
            self.led_button.configure(text="LED OFF")
        else:
            self.led_button.configure(text="LED ON")

        self.response_var.set(response)

    def enter_pressed(self, event) -> None:
        self.send_custom_command()

    def close_application(self) -> None:
        self.disconnect_device()
        self.root.destroy()


def main() -> None:
    root = tk.Tk()
    app = EnvironmentMonitorApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
