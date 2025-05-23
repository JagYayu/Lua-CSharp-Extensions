--- @meta

--- @class StringBuffer : userdata
local StringBuffer = {}

--- @param value any
--- @return StringBufferData
function StringBuffer:encode(value) end

--- @param value any
--- @return string
function StringBuffer:encodeString(value) end

--- @param data StringBufferData
--- @return any
function StringBuffer:decode(data) end

--- @param str string
--- @return any
function StringBuffer:decode(str) end

return StringBuffer
