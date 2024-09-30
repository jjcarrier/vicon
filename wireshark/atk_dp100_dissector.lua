-- ATK-DP100 USB HID Dissector for Wireshark
-- Most details were extracted via the use of ILSpy with initial/supplemental
-- information obtained from:
-- https://github.com/palzhj/pydp100/blob/main/powerread.py
-- Simple Wireshark Filter Expression (for OP_BASICSET):  (usb.idProduct == 0xaf01) || (usbhid.op_code == 0x35)

local usb_hid_proto = Proto("ATK-DP100", "ATK-DP100 HID")

local IF_CLASS_UNKNOWN  <const> = 0xFFFF
local IF_CLASS_HID      <const> = 0x0003

local DIR_D2H           <const> = 0xFA
local DIR_H2D           <const> = 0xFB

local SET_NACK          <const> = 0x00
local SET_ACK           <const> = 0x01

local OP_NONE           <const> = 0x00
local OP_DEVICE_INFO    <const> = 0x10

-- Firmware upgrade related
local OP_FW_INFO        <const> = 0x11
local OP_START_TRANS    <const> = 0x12
local OP_DATA_TRANS     <const> = 0x13
local OP_END_TRANS      <const> = 0x14
local OP_DEV_UPGRADE    <const> = 0x15

local OP_BASICINFO      <const> = 0x30
local OP_BASICSET       <const> = 0x35
local OP_SYSTEMINFO     <const> = 0x40
local OP_SYSTEMSET      <const> = 0x45
local OP_SCANOUT        <const> = 0x50
local OP_SERIALOUT      <const> = 0x55
local OP_DISCONNECT     <const> = 0x80

-- Upper 4-bits of OP_BASICSET determine sub-operation:
-- (NOTE: SET_CURRENT_BASIC is related to OpenOut/CloseOut which uses odd/backwards terminology,
-- "Open" == Output ON, "Close" == Output OFF)
local OP_BASICSET_GET_GROUP_INFO    <const> = 0x00
local OP_BASICSET_SET_CURRENT_BASIC <const> = 0x20
local OP_BASICSET_SAVE_GROUP        <const> = 0x60 -- This is a guess for the name.
local OP_BASICSET_GET_CURRENT_BASIC <const> = 0x80
local OP_BASICSET_USE_GROUP         <const> = 0xA0

-- Define the fields to be parsed
local fields = usb_hid_proto.fields
fields.direction = ProtoField.uint8("usbhid.direction", "Direction", base.HEX)
fields.op_code = ProtoField.uint8("usbhid.op_code", "OpCode", base.HEX)
fields.pad = ProtoField.uint8("usbhid.pad", "Pad", base.HEX)
fields.data_len = ProtoField.uint8("usbhid.data_len", "Data Length", base.DEC)
fields.data = ProtoField.bytes("usbhid.data", "Data")
fields.crc = ProtoField.uint16("usbhid.crc", "CRC16", base.HEX)

-- Dissector function
function usb_hid_proto.dissector(buffer, pinfo, tree)
    pinfo.cols.protocol = usb_hid_proto.name

    local direction = buffer(0, 1):uint()
    local op_code   = buffer(1, 1):uint()
    local pad       = buffer(2, 1):uint()
    local data_len  = buffer(3, 1):uint()
    local data      = buffer(4, data_len)
    local crc       = buffer(4 + data_len, 2):le_uint()

    local subtree = tree:add(usb_hid_proto, buffer(), "ATK-DP100 HID")
    subtree:add(fields.direction, buffer(0, 1))
    subtree:add(fields.op_code, buffer(1, 1))
    -- TODO process the op_code and generate a more descriptive entry here
    subtree:add(fields.pad, buffer(2, 1))
    subtree:add(fields.data_len, buffer(3, 1))

    if data_len > 0 then
        subtree:add(fields.data, buffer(4, data_len))
    end

    if direction == DIR_H2D and op_code == OP_BASICINFO then
        subtree:add(buffer(1, 1), "Op: BasicInfo")

    elseif direction == DIR_D2H and op_code == OP_BASICINFO then
        subtree:add(buffer(1, 1), "Op: BasicInfo")

        -- Parse data fields from data buffer.
        local vin          = data(0, 2):le_uint()
        local vout         = data(2, 2):le_uint()
        local iout         = data(4, 2):le_uint()
        local vout_max     = data(6, 2):le_uint()
        local temperature1 = data(8, 2):le_uint() / 10
        local temperature2 = data(10, 2):le_uint() / 10
        local v_usb        = data(12, 2):le_uint()
        local mode         = data(14, 1):uint()
        local status       = data(15, 1):uint()

        local subtree_basic_info = subtree:add(data(), "Response")

        -- Label and set data selection assignments.
        subtree_basic_info:add(buffer(4 + 0, 2), "Input Voltage: " .. vin .. " mV")
        subtree_basic_info:add(buffer(4 + 2, 2), "Output Voltage: " .. vout .. " mV")
        subtree_basic_info:add(buffer(4 + 4, 2), "Output Current: " .. iout .. " mA")
        subtree_basic_info:add(buffer(4 + 6,  2), "Output Voltage (Max): " .. vout_max .. " mV")
        subtree_basic_info:add(buffer(4 + 8,  2), "Temperature: " .. temperature1 .. " C")
        subtree_basic_info:add(buffer(4 + 10, 2), "Temperature: " .. temperature2 .. " C")
        subtree_basic_info:add(buffer(4 + 12, 2), "USB 5V: " .. v_usb .. " mV")
        subtree_basic_info:add(buffer(4 + 14, 1), "Mode: " .. mode)
        subtree_basic_info:add(buffer(4 + 15, 1), "Status: " .. status)

    elseif op_code == OP_FW_INFO then -- TODO: which direction is this?
        subtree:add(buffer(1, 1), "Op: FwInfo")

        -- Parse data fields from data buffer.
        local dev_type = data(0, 16):stringz()
        local data_len = data(16, 1):uint()
        local enc_pos  = data(17, 1):uint()
        local hw_ver   = data(18, 2):le_uint()
        local app_ver  = data(20, 2):le_uint()
        local bin_crc  = data(22, 2):le_uint()
        local bin_size = data(24, 4):le_uint()
        local year     = data(28, 2):le_uint()
        local month    = data(30, 1):uint()
        local day      = data(31, 1):uint()

        local subtree_basic_info = subtree:add(data(), "Response")

        -- Label and set data selection assignments.
        subtree_dev_info:add(buffer(4 + 0, 16), "Device: " .. dev_type)

        subtree_dev_info:add(buffer(4 + 16, 2), "Data length: " .. data_len)
        subtree_dev_info:add(buffer(4 + 17, 2), "Encrpt Pos: " .. enc_pos)

        subtree_dev_info:add(buffer(4 + 18, 2), "HW Version: " .. hw_ver / 10)
        subtree_dev_info:add(buffer(4 + 20, 2), "APP Version: " .. app_ver / 10)

        subtree_dev_info:add(buffer(4 + 22, 2), "BIN CRC16: " .. bin_crc)
        subtree_dev_info:add(buffer(4 + 24, 2), "BIN Size: " .. bin_size)

        subtree_dev_info:add(buffer(4 + 28, 2), "Year: " .. year)
        subtree_dev_info:add(buffer(4 + 30, 1), "Month: " .. month)
        subtree_dev_info:add(buffer(4 + 31, 1), "Day: " .. day)

    elseif op_code == OP_BASICSET then
        subtree:add(buffer(1, 1), "Op: BasicSet")

        if direction == DIR_D2H then
            local subtree_basic_set = subtree:add(data(), "Response")

            if data_len == 1 then
                local ack = data(0, 1):uint()

                -- Label and set data selection assignments.
                if (ack == SET_ACK) then
                    subtree_basic_set:add(buffer(4 + 0, 1), "Set: ACK")
                else
                    subtree_basic_set:add(buffer(4 + 0, 1), "Set: NACK")
                end
            else
                -- Parse data fields from data buffer.
                local index = data(0, 1):uint()
                local state = data(1, 1):uint()
                local v_set = data(2, 2):le_uint()
                local i_set = data(4, 2):le_uint()
                local ovp   = data(6, 2):le_uint()
                local ocp   = data(8, 2):le_uint()

                if (index & 0xF0) == OP_BASICSET_GET_GROUP_INFO then
                    subtree_basic_set:add(buffer(4 + 0, 1), "SubOpCode: GetGroupInfo")
                elseif (index & 0xF0) == OP_BASICSET_SET_CURRENT_BASIC then
                    subtree_basic_set:add(buffer(4 + 0, 1), "SubOpCode: SetCurrentBasic")
                elseif (index & 0xF0) == OP_BASICSET_SAVE_GROUP then
                    subtree_basic_set:add(buffer(4 + 0, 1), "SubOpCode: SaveGroup")
                elseif (index & 0xF0) == OP_BASICSET_GET_CURRENT_BASIC then
                    subtree_basic_set:add(buffer(4 + 0, 1), "SubOpCode: GetCurrentBasic")
                elseif (index & 0xF0) == OP_BASICSET_USE_GROUP then
                    subtree_basic_set:add(buffer(4 + 0, 1), "SubOpCode: UseGroup")
                end

                -- Label and set data selection assignments.
                subtree_basic_set:add(buffer(4 + 0, 1), "Preset Index: " .. (index & 0x0F))
                subtree_basic_set:add(buffer(4 + 1, 1), "Output State: " .. state)
                subtree_basic_set:add(buffer(4 + 2, 2), "VSET: " .. v_set .. " mV")
                subtree_basic_set:add(buffer(4 + 4, 2), "ISET: " .. i_set .. " mA")
                subtree_basic_set:add(buffer(4 + 6, 2), "OVP: " .. ovp .. " mV")
                subtree_basic_set:add(buffer(4 + 8, 2), "OCP: " .. ocp .. " mA")

            end
        elseif direction == DIR_H2D then
            -- This opcode changes meaning based on the data length,
            -- or rather the first byte's most-significant bit (if set, it means
            -- set preset index operation).
            local index = data(0, 1):uint()

            local subtree_basic_set = subtree:add(data(), "Command")

            if (index & 0xF0) == OP_BASICSET_GET_GROUP_INFO then
                -- Only one byte of data
                subtree_basic_set:add(buffer(4 + 0, 1), "SubOpCode: GetGroupInfo")
                subtree_basic_set:add(buffer(4 + 0, 1), "Preset Index: " .. (index & 0x0F))
            elseif (index & 0xF0) == OP_BASICSET_GET_CURRENT_BASIC then
                subtree_basic_set:add(buffer(4 + 0, 1), "SubOpCode: GetCurrentBasic")
                subtree_basic_set:add(buffer(4 + 0, 1), "Preset Index: " .. (index & 0x0F))
            elseif (index & 0xF0) == OP_BASICSET_USE_GROUP or
                   (index & 0xF0) == OP_BASICSET_SET_CURRENT_BASIC or
                   (index & 0xF0) == OP_BASICSET_SAVE_GROUP then
                -- Parse data fields from data buffer.
                local state = data(1, 1):uint()
                local v_set = data(2, 2):le_uint()
                local i_set = data(4, 2):le_uint()
                local ovp   = data(6, 2):le_uint()
                local ocp   = data(8, 2):le_uint()

                -- Label and set data selection assignments.
                if (index & 0xF0) == OP_BASICSET_SET_CURRENT_BASIC then
                    subtree_basic_set:add(buffer(4 + 0, 1), "SubOpCode: SetCurrentBasic")
                elseif (index & 0xF0) == OP_BASICSET_USE_GROUP then
                    subtree_basic_set:add(buffer(4 + 0, 1), "SubOpCode: UseGroup")
                elseif (index & 0xF0) == OP_BASICSET_SAVE_GROUP then
                    subtree_basic_set:add(buffer(4 + 0, 1), "SubOpCode: SaveGroup")
                end

                subtree_basic_set:add(buffer(4 + 0, 1), "Preset Index: " .. (index & 0x0F))
                subtree_basic_set:add(buffer(4 + 1, 1), "Output State: " .. state)
                subtree_basic_set:add(buffer(4 + 2, 2), "VSET: " .. v_set .. " mV")
                subtree_basic_set:add(buffer(4 + 4, 2), "ISET: " .. i_set .. " mA")
                subtree_basic_set:add(buffer(4 + 6, 2), "OVP: " .. ovp .. " mV")
                subtree_basic_set:add(buffer(4 + 8, 2), "OCP: " .. ocp .. " mA")
            end
        end

    elseif direction == DIR_D2H and op_code == OP_DEVICE_INFO then
        subtree:add(buffer(1, 1), "Op: DeviceInfo")

        -- Parse data fields from data buffer.
        local dev_type = data(0, 16):stringz()
        local hw_ver   = data(16, 2):le_uint()
        local app_ver  = data(18, 2):le_uint()
        local boot_ver = data(20, 2):le_uint()
        local run_area = data(22, 2):le_uint()
        local dev_sn   = data(32, 4) -- ATK_P400.USBHID.DevInfo claims this may be up to 12 bytes.
        local year     = data(36, 2):le_uint()
        local month    = data(38, 1):uint()
        local day      = data(39, 1):uint()

        local subtree_dev_info = subtree:add(data(), "Response")

        -- Label and set data selection assignments.
        subtree_dev_info:add(buffer(4 + 0, 16), "Device: " .. dev_type)
        subtree_dev_info:add(buffer(4 + 16, 2), "HW Version: " .. hw_ver / 10)
        subtree_dev_info:add(buffer(4 + 18, 2), "APP Version: " .. app_ver / 10)
        subtree_dev_info:add(buffer(4 + 20, 2), "BOOT Version: " .. boot_ver / 10)
        subtree_dev_info:add(buffer(4 + 22, 2), "Run Area: " .. run_area)
        subtree_dev_info:add(buffer(4 + 32, 4), "Serial Num: " .. dev_sn)
        subtree_dev_info:add(buffer(4 + 36, 2), "Year: " .. year)
        subtree_dev_info:add(buffer(4 + 38, 1), "Month: " .. month)
        subtree_dev_info:add(buffer(4 + 39, 1), "Day: " .. day)

    elseif op_code == OP_SYSTEMINFO then
        subtree:add(buffer(1, 1), "Op: SystemInfo")

        if data_len == 0 then
            -- Presumably, this is a read-request
        elseif data_len == 1 then
            local subtree_sys_info = subtree:add(data(), "Response")
            local ack = data(0, 1):uint()

            -- Label and set data selection assignments.
            if (ack == SET_ACK) then
                subtree_sys_info:add(buffer(4 + 0, 1), "Set: ACK")
            else
                subtree_sys_info:add(buffer(4 + 0, 1), "Set: NACK")
            end
        else
            local subtree_sys_info

            if direction == DIR_D2H then
                subtree_sys_info = subtree:add(data(), "Response")
            else
                subtree_sys_info = subtree:add(data(), "Command")
            end
            -- Parse data fields from data buffer.
            local otp       = data(0, 2):le_uint()
            local opp       = data(2, 2):le_uint() / 10
            local backlight = data(4, 1):uint()
            local volume    = data(5, 1):uint()

            -- Label and set data selection assignments.
            subtree_sys_info:add(buffer(4 + 0, 2), "OTP: " .. otp)
            subtree_sys_info:add(buffer(4 + 2, 2), "OPP: " .. opp)
            subtree_sys_info:add(buffer(4 + 4, 1), "Backlight: " .. backlight)
            subtree_sys_info:add(buffer(4 + 5, 1), "Volume: " .. volume)

            if data_len == 8 then
                local rpp       = data(6, 1):uint()
                local auto_on   = data(7, 1):uint()
                subtree_sys_info:add(buffer(4 + 6, 1), "RPP: " .. rpp)
                subtree_sys_info:add(buffer(4 + 7, 1), "Auto_ON: " .. auto_on)
            end
        end
    end

    subtree:add(fields.crc, buffer(4 + data_len, 2))
end

-- Register the dissector
DissectorTable.get("usb.interrupt"):add(IF_CLASS_HID, usb_hid_proto)
