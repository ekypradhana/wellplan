select 
CUSTOMER_NUM as _id,
SERVICE_CLASS,
UNBAR_DATE,
UNBAR_DATE + Cast(Cast(UNBAR_TIME as Time) as DateTime) as UNBAR_TIME,
DEALER_NUM,
DEALER_NAME,
RETAILER_NUM,
RETAILER_NAME,
DEALER_CITY,
DEALER_ZONE,
FOS_NAME,
FES_NUM,
RETAILER_SMS_DATE,
RETAILER_SMS_DATE + Cast(Cast(RETAILER_SMS_TIME as Time) as DateTime) as RETAILER_SMS_TIME,
HUB,
RETAILER_CITY,
CIRCLE_ID,
ACTIVATION_DATE,
ACTIVATION_DATE + Cast(Cast(ACTIVATION_TIME as Time) as DateTime) as ACTIVATION_TIME,
TV_STATUS_DATE,
TV_STATUS_DATE + Cast(Cast(TV_STATUS_TIME as Time) as DateTime) as TV_STATUS_TIME 
from FTA