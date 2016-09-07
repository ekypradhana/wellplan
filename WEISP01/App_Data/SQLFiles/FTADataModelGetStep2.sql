select 
	'Data' as USAGE_TYPE, '' as REF1, '' as REF2, 
	--CALL_START_DATE + Cast(Cast(CALL_START_TIME as Time) as DateTime) as CALL_DATE_TIME,
	sum(CALL_EVENT_DURATION) / (count(distinct(CALL_START_DATE)) * 24 * count(distinct ACCESS_METHOD_IDENTIFIER)) as DURATION,
	sum(TOTAL_VOLUME) / (count(distinct(CALL_START_DATE)) * 24 * count(distinct ACCESS_METHOD_IDENTIFIER)) as VOLUME
from SASN 
UNION 
select 
	'Call' as USAGE_TYPE, '' as REF1, '' as REF2, 
	--CALL_START_DATE + Cast(Cast(CALL_START_TIME as Time) as DateTime) as CALL_DATE_TIME,
	sum(CALL_DURATION) / (count(distinct(CALL_START_DATE)) * 24 * count(distinct ACCESS_METHOD_IDENTIFIER)) as DURATION,
	0 as VOLUME
from MSC 
