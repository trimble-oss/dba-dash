SELECT CONVERT(CHAR(16), base_address, 2) base_address_string,
       file_version,
       product_version,
       debug,
       patched,
       prerelease,
       private_build,
       special_build,
       language,
       company,
       description,
       name
FROM sys.dm_os_loaded_modules