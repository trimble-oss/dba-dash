﻿CREATE FUNCTION dbo.Histogram_CPU(@CPU INT)
RETURNS TABLE
AS
RETURN 
SELECT CASE WHEN @CPU<10 THEN 1 ELSE 0 END AS CPU10,
	   CASE WHEN @CPU >=10 AND @CPU <20 THEN 1 ELSE 0 END AS CPU20,
	   CASE WHEN @CPU >=20 AND @CPU <30 THEN 1 ELSE 0 END AS CPU30,
	   CASE WHEN @CPU >=30 AND @CPU <40 THEN 1 ELSE 0 END AS CPU40,
	   CASE WHEN @CPU >=40 AND @CPU <50 THEN 1 ELSE 0 END AS CPU50,
	   CASE WHEN @CPU >=50 AND @CPU <60 THEN 1 ELSE 0 END AS CPU60,
	   CASE WHEN @CPU >=60 AND @CPU <70 THEN 1 ELSE 0 END AS CPU70,
	   CASE WHEN @CPU >=70 AND @CPU <80 THEN 1 ELSE 0 END AS CPU80,
	   CASE WHEN @CPU >=80 AND @CPU <90 THEN 1 ELSE 0 END AS CPU90,
	   CASE WHEN @CPU>= 90 THEN 1 ELSE 0 END AS CPU100