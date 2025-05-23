--- @meta

--- @class Table : userdata
local Table = {}

--- @param table table
--- @param begin integer
--- @param length integer
function Table.arrayCopy(table, begin, length) end

--- @param table table
function Table.getArrayCapacity(table) end

--- @param table table
function Table.getHashMapCount(table) end

--- @param arrayCapacity integer
--- @param hashMapCapacity integer
function Table.NewTable(arrayCapacity, hashMapCapacity) end

return Table
