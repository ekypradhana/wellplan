select * from (
select 
	coalesce(s.MOBILE_NO, p.ACCESS_METHOD_IDENTIFIER) _id,
	isnull(BEG_SERV_DATE, '1900-01-01') as BEG_SERV_DATE,
	isnull(CUST_STATUS,1) as CUST_STATUS,
	isnull(s.SEGMENT,'N/A') as CUSTOMER_SEGMENT,
	isnull(p.PRE_POST_INDICATOR,1) as PRE_POST_INDICATOR,
	--isnull(p.DOB,GETDATE()) as DOB,
	isnull(p.COMPANY_NAME,'N/A') as COMPANY_NAME,
	isnull(p.SUBS_ACTIVATION_DATE, '1900-01-01') as SUBS_ACTIVATION_DATE,
	isnull(p.SUBSC_CURRENT_STATUS_CODE, 0) as SUB_STATUS_CODE,
	isnull(p.PREF_BILL_LANGUAGE, '') as PREF_BILL_LANGUAGE
from CustomerSegmentations s 
full join CustomerProfiles p on s.MOBILE_NO = p.ACCESS_METHOD_IDENTIFIER 
) custs 