CREATE TYPE [dbo].[OSLoadedModules] AS TABLE (
    [base_address_string] CHAR (16)      NOT NULL,
    [file_version]        VARCHAR (256)  NULL,
    [product_version]     VARCHAR (256)  NULL,
    [debug]               BIT            NULL,
    [patched]             BIT            NULL,
    [prerelease]          BIT            NULL,
    [private_build]       BIT            NULL,
    [special_build]       BIT            NULL,
    [language]            INT            NULL,
    [company]             NVARCHAR (256) NULL,
    [description]         NVARCHAR (256) NULL,
    [name]                NVARCHAR (255) NOT NULL,
    PRIMARY KEY CLUSTERED ([name] ASC));





