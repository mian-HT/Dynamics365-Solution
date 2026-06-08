ô
`D:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\ChannelContracts\Payload.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
.! "
ChannelContracts" 2
{ 
[

 
DataContract

 
]

 
public 

class 
Payload 
{ 
[ 	

DataMember	 
] 
public 
Guid 
ChannelDefinitionId '
{( )
get* -
;- .
set/ 2
;2 3
}4 5
[ 	

DataMember	 
] 
public 
string 
ChannelInstanceId '
{( )
get* -
;- .
set/ 2
;2 3
}4 5
[ 	

DataMember	 
] 
public 
string 
	RequestId 
{  !
get" %
;% &
set' *
;* +
}, -
[ 	

DataMember	 
] 
public 
string 
From 
{ 
get  
;  !
set" %
;% &
}' (
[ 	

DataMember	 
] 
public 
string 
To 
{ 
get 
; 
set  #
;# $
}% &
[ 	

DataMember	 
] 
public 
IDictionary 
< 
string !
,! "
string# )
>) *
Message+ 2
{3 4
get5 8
;8 9
set: =
;= >
}? @
=A B
newC F

DictionaryG Q
<Q R
stringR X
,X Y
stringZ `
>` a
(a b
)b c
;c d
} 
} î
aD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\ChannelContracts\Response.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
.! "
ChannelContracts" 2
{ 
[

 
DataContract

 
]

 
public 

class 
Response 
{ 
[ 	

DataMember	 
] 
public 
Guid 
ChannelDefinitionId '
{( )
get* -
;- .
set/ 2
;2 3
}4 5
[ 	

DataMember	 
] 
public 
string 
	RequestId 
{  !
get" %
;% &
set' *
;* +
}, -
[ 	

DataMember	 
] 
public 
string 
	MessageId 
{  !
get" %
;% &
set' *
;* +
}, -
[ 	

DataMember	 
] 
public 
string 
Status 
{ 
get "
;" #
set$ '
;' (
}) *
[ 	

DataMember	 
] 
public 

Dictionary 
< 
string  
,  !
object" (
>( )
StatusDetails* 7
{8 9
get: =
;= >
set? B
;B C
}D E
} 
} Ω
tD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\ConsumingApplicationContracts\DeliveryReport.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
{ 
[ 
DataContract 
] 
public 

class 
DeliveryReport 
{		 
[

 	

DataMember

	 
]

 
public 
Guid 
ChannelDefinitionId '
{( )
get* -
;- .
set/ 2
;2 3
}4 5
[ 	

DataMember	 
] 
public 
string 
	RequestId 
{  !
get" %
;% &
set' *
;* +
}, -
[ 	

DataMember	 
] 
public 
string 
	MessageId 
{  !
get" %
;% &
set' *
;* +
}, -
[ 	

DataMember	 
] 
public 
string 
Status 
{ 
get "
;" #
set$ '
;' (
}) *
[ 	

DataMember	 
] 
public 
IDictionary 
< 
string !
,! "
object# )
>) *
StatusDetails+ 8
{9 :
get; >
;> ?
set@ C
;C D
}E F
[ 	

DataMember	 
] 
public 
string 
From 
{ 
get  
;  !
set" %
;% &
}' (
[ 	

DataMember	 
] 
public 
string 
OrganizationId $
{% &
get' *
;* +
internal, 4
set5 8
;8 9
}: ;
} 
} Ê]
\D:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\DeliveryReportPlugin.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
{ 
public

 

class

  
DeliveryReportPlugin

 %
:

& '
IPlugin

( /
{ 
private 
static 
readonly 
Guid  $
ChannelDefinitionId% 8
=9 :
Guid; ?
.? @
Parse@ E
(E F
$strF l
)l m
;m n
public 
void 
Execute 
( 
IServiceProvider ,
serviceProvider- <
)< =
{ 	
var 
tracingService 
=  
serviceProvider! 0
.0 1
Get1 4
<4 5
ITracingService5 D
>D E
(E F
)F G
;G H
tracingService 
. 
Trace  
(  !
$str! C
)C D
;D E
var "
pluginExecutionContext &
=' (
serviceProvider) 8
.8 9
Get9 <
<< =#
IPluginExecutionContext= T
>T U
(U V
)V W
;W X
var 
payload 
= "
pluginExecutionContext 0
.0 1
InputParameters1 @
[@ A
$strA J
]J K
asL N
stringO U
;U V
tracingService 
. 
Trace  
(  !
payload! (
)( )
;) *
var 
organizationService #
=$ %
serviceProvider& 5
.5 6
Get6 9
<9 :'
IOrganizationServiceFactory: U
>U V
(V W
)W X
.X Y%
CreateOrganizationServiceY r
(r s
nulls w
)w x
;x y
var 
queryParams 
= 
ParseQueryString .
(. /
payload/ 6
)6 7
;7 8
queryParams 
. 
TryGetValue #
(# $
$str$ 0
,0 1
out2 5
var6 9
rawExternalId: G
)G H
;H I
queryParams 
. 
TryGetValue #
(# $
$str$ +
,+ ,
out- 0
var1 4
state5 :
): ;
;; <
queryParams 
. 
TryGetValue #
(# $
$str$ /
,/ 0
out1 4
var5 8
	messageID9 B
)B C
;C D
var 

fromNumber 
= 
string #
.# $
Empty$ )
;) *
var 
d365MessageId 
=  
ResolveD365MessageId  4
(4 5
rawExternalId5 B
,B C
tracingServiceD R
,R S
refT W

fromNumberX b
)b c
;c d
var   
deliveryReport   
=    
new  ! $
DeliveryReport  % 3
(  3 4
)  4 5
{!! 
ChannelDefinitionId"" #
=""$ %
ChannelDefinitionId""& 9
,""9 :
	MessageId## 
=## 
	messageID## %
,##% &
	RequestId$$ 
=$$ 
d365MessageId$$ )
,$$) *
Status%% 
=%% 
MapState%% !
(%%! "
state%%" '
)%%' (
,%%( )
From&& 
=&& 

fromNumber&& !
,&&! "
OrganizationId'' 
=''  "
pluginExecutionContext''! 7
.''7 8
OrganizationId''8 F
.''F G
ToString''G O
(''O P
)''P Q
,''Q R
StatusDetails(( 
=(( 
new((  #

Dictionary(($ .
<((. /
string((/ 5
,((5 6
object((7 =
>((= >
(((> ?
)((? @
})) 
;)) 
var++ 
notificatonPayload++ "
=++# $
	JsonUtils++% .
.++. /
	Serialize++/ 8
(++8 9
deliveryReport++9 G
)++G H
;++H I
tracingService,, 
.,, 
Trace,,  
(,,  !
$str,,! <
,,,< =
notificatonPayload,,> P
),,P Q
;,,Q R
var.. 
response.. 
=.. 
organizationService.. .
.... /
Execute../ 6
(..6 7
new..7 :
OrganizationRequest..; N
(..N O
$str..O o
)..o p
{// 

Parameters00 
=00 
{11 
{22 
$str22 +
,22+ ,
notificatonPayload22- ?
}22@ A
}33 
}44 
)44 
;44 "
pluginExecutionContext66 "
.66" #
OutputParameters66# 3
[663 4
$str664 >
]66> ?
=66@ A
response66B J
.66J K
Results66K R
[66R S
$str66S d
]66d e
;66e f
}77 	
private:: 
static:: 

Dictionary:: !
<::! "
string::" (
,::( )
string::* 0
>::0 1
ParseQueryString::2 B
(::B C
string::C I
payload::J Q
)::Q R
{;; 	
var<< 
queryParams<< 
=<< 
new<< !

Dictionary<<" ,
<<<, -
string<<- 3
,<<3 4
string<<5 ;
><<; <
(<<< =
StringComparer<<= K
.<<K L
OrdinalIgnoreCase<<L ]
)<<] ^
;<<^ _
if== 
(== 
string== 
.== 
IsNullOrEmpty== $
(==$ %
payload==% ,
)==, -
)==- .
{>> 
return?? 
queryParams?? "
;??" #
}@@ 
foreachBB 
(BB 
varBB 
pairBB 
inBB  
payloadBB! (
.BB( )
SplitBB) .
(BB. /
$charBB/ 2
)BB2 3
)BB3 4
{CC 
varDD 
idxDD 
=DD 
pairDD 
.DD 
IndexOfDD &
(DD& '
$charDD' *
)DD* +
;DD+ ,
ifEE 
(EE 
idxEE 
<EE 
$numEE 
)EE 
{FF 
continueGG 
;GG 
}HH 
varJJ 
keyJJ 
=JJ 
UriJJ 
.JJ 
UnescapeDataStringJJ 0
(JJ0 1
pairJJ1 5
.JJ5 6
	SubstringJJ6 ?
(JJ? @
$numJJ@ A
,JJA B
idxJJC F
)JJF G
)JJG H
.JJH I
TrimJJI M
(JJM N
)JJN O
;JJO P
varKK 
valueKK 
=KK 
UriKK 
.KK  
UnescapeDataStringKK  2
(KK2 3
pairKK3 7
.KK7 8
	SubstringKK8 A
(KKA B
idxKKB E
+KKF G
$numKKH I
)KKI J
)KKJ K
.KKK L
TrimKKL P
(KKP Q
)KKQ R
;KKR S
queryParamsLL 
[LL 
keyLL 
]LL  
=LL! "
valueLL# (
;LL( )
}MM 
returnOO 
queryParamsOO 
;OO 
}PP 	
privateSS 
staticSS 
stringSS  
ResolveD365MessageIdSS 2
(SS2 3
stringSS3 9
rawExternalIdSS: G
,SSG H
ITracingServiceSSI X
tracingServiceSSY g
,SSg h
refSSi l
stringSSm s

fromNumberSSt ~
)SS~ 
{TT 	
varUU 
d365MessageIdUU 
=UU 
rawExternalIdUU  -
;UU- .
ifVV 
(VV 
stringVV 
.VV 
IsNullOrEmptyVV $
(VV$ %
rawExternalIdVV% 2
)VV2 3
||VV4 6
!VV7 8
rawExternalIdVV8 E
.VVE F
ContainsVVF N
(VVN O
$strVVO R
)VVR S
)VVS T
{WW 
returnXX 
d365MessageIdXX $
;XX$ %
}YY 
var[[ 
parts[[ 
=[[ 
rawExternalId[[ %
.[[% &
Split[[& +
([[+ ,
$char[[, /
)[[/ 0
;[[0 1
try\\ 
{]] 
var^^ 
msgB64^^ 
=^^ 
parts^^ "
[^^" #
$num^^# $
]^^$ %
.^^% &
Replace^^& -
(^^- .
$str^^. 1
,^^1 2
$str^^3 6
)^^6 7
.^^7 8
Replace^^8 ?
(^^? @
$str^^@ C
,^^C D
$str^^E H
)^^H I
;^^I J
var__ 
padding__ 
=__ 
msgB64__ $
.__$ %
Length__% +
%__, -
$num__. /
;__/ 0
if`` 
(`` 
padding`` 
>`` 
$num`` 
)``  
{aa 
msgB64bb 
+=bb 
newbb !
stringbb" (
(bb( )
$charbb) ,
,bb, -
$numbb. /
-bb0 1
paddingbb2 9
)bb9 :
;bb: ;
}cc 
d365MessageIdee 
=ee 
newee  #
Guidee$ (
(ee( )
Convertee) 0
.ee0 1
FromBase64Stringee1 A
(eeA B
msgB64eeB H
)eeH I
)eeI J
.eeJ K
ToStringeeK S
(eeS T
)eeT U
;eeU V
}ff 
catchgg 
(gg 
	Exceptiongg 
exgg 
)gg  
whengg! %
(gg& '
exgg' )
isgg* ,
FormatExceptiongg- <
||gg= ?
exgg@ B
isggC E
ArgumentExceptionggF W
)ggW X
{hh 
tracingServicejj 
.jj 
Tracejj $
(jj$ %
$strjj% >
+jj? @
exjjA C
.jjC D
MessagejjD K
)jjK L
;jjL M
}kk 
ifmm 
(mm 
partsmm 
.mm 
Lengthmm 
>=mm 
$nummm  !
)mm! "
{nn 

fromNumberoo 
=oo 
Urioo  
.oo  !
UnescapeDataStringoo! 3
(oo3 4
partsoo4 9
[oo9 :
$numoo: ;
]oo; <
)oo< =
;oo= >
}pp 
returnrr 
d365MessageIdrr  
;rr  !
}ss 	
privateuu 
staticuu 
stringuu 
MapStateuu &
(uu& '
stringuu' -
stateuu. 3
)uu3 4
{vv 	
ifww 
(ww 
stringww 
.ww 
Equalsww 
(ww 
stateww #
,ww# $
$strww% .
,ww. /
StringComparisonww0 @
.ww@ A
OrdinalIgnoreCasewwA R
)wwR S
)wwS T
{xx 
returnyy 
$stryy "
;yy" #
}zz 
if|| 
(|| 
string|| 
.|| 
Equals|| 
(|| 
state|| #
,||# $
$str||% .
,||. /
StringComparison||0 @
.||@ A
OrdinalIgnoreCase||A R
)||R S
||}} 
string}} 
.}} 
Equals}}  
(}}  !
state}}! &
,}}& '
$str}}( 2
,}}2 3
StringComparison}}4 D
.}}D E
OrdinalIgnoreCase}}E V
)}}V W
)}}W X
{~~ 
return 
$str 
;  
}
ÄÄ 
return
ÇÇ 
$str
ÇÇ 
;
ÇÇ 
}
ÉÉ 	
}
ÑÑ 
}ÖÖ ÷
UD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\InboundPlugin.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
{ 
public		 

class		 
InboundPlugin		 
:		  
IPlugin		! (
{

 
public 
void 
Execute 
( 
IServiceProvider ,
serviceProvider- <
)< =
{ 	
var 
tracingService 
=  
serviceProvider! 0
.0 1
Get1 4
<4 5
ITracingService5 D
>D E
(E F
)F G
;G H
tracingService 
. 
Trace  
(  !
$str! ;
); <
;< =
var "
pluginExecutionContext &
=' (
serviceProvider) 8
.8 9
Get9 <
<< =#
IPluginExecutionContext= T
>T U
(U V
)V W
;W X
var 
payload 
= "
pluginExecutionContext 0
.0 1
InputParameters1 @
[@ A
$strA J
]J K
asL N
stringO U
;U V
tracingService 
. 
Trace  
(  !
payload! (
)( )
;) *
var 
organizationService #
=$ %
serviceProvider& 5
.5 6
Get6 9
<9 :'
IOrganizationServiceFactory: U
>U V
(V W
)W X
.X Y%
CreateOrganizationServiceY r
(r s
nulls w
)w x
;x y
var 
response 
= 
organizationService .
.. /
Execute/ 6
(6 7
new7 :
OrganizationRequest; N
(N O
$strO j
)j k
{ 

Parameters 
= 
{ 
{ 
$str %
,% &
payload' .
}. /
} 
} 
) 
; "
pluginExecutionContext "
." #
OutputParameters# 3
[3 4
$str4 >
]> ?
=@ A
responseB J
.J K
ResultsK R
[R S
$strS d
]d e
;e f
} 	
} 
} · 
QD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\JsonUtils.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
{ 
public 

static 
class 
	JsonUtils !
{ 
public 
static 
string 
	Serialize &
<& '
T' (
>( )
() *
T* +
entity, 2
,2 3.
"DataContractJsonSerializerSettings4 V
settingsW _
=` a
nullb f
)f g
{ 	
using 
( 
var 
stream 
= 
new  #
MemoryStream$ 0
(0 1
)1 2
)2 3
using 
( 
var 
streamReader #
=$ %
new& )
StreamReader* 6
(6 7
stream7 =
)= >
)> ?
{ 
var 
serializerSettings &
=' (
settings) 1
??) +
new, /.
"DataContractJsonSerializerSettings0 R
{) *%
UseSimpleDictionaryFormat- F
=G H
trueI M
,M N

KnownTypes- 7
=8 9
new: =
[= >
]> ?
{@ A
typeofB H
(H I
stringI O
[O P
]P Q
)Q R
,R S
typeofT Z
(Z [
List[ _
<_ `
object` f
>f g
)g h
}i j
}) *
;* +
var 

serializer 
=  
new! $&
DataContractJsonSerializer% ?
(? @
typeof@ F
(F G
TG H
)H I
,I J
serializerSettingsK ]
)] ^
;^ _

serializer 
. 
WriteObject &
(& '
stream' -
,- .
entity/ 5
)5 6
;6 7
stream 
. 
Position 
=  !
$num" #
;# $
return 
streamReader #
.# $
	ReadToEnd$ -
(- .
). /
;/ 0
} 
} 	
public!! 
static!! 
T!! 
Deserialize!! #
<!!# $
T!!$ %
>!!% &
(!!& '
string!!' -
json!!. 2
,!!2 3.
"DataContractJsonSerializerSettings!!4 V
settings!!W _
=!!` a
null!!b f
)!!f g
{"" 	
if## 
(## 
typeof## 
(## 
T## 
)## 
==## 
typeof## #
(### $
string##$ *
)##* +
)##+ ,
{$$ 
return%% 
(%% 
T%% 
)%% 
(%% 
object%% !
)%%! "
json%%" &
;%%& '
}&& 
using(( 
((( 
var(( 
memoryStream(( #
=(($ %
new((& )
MemoryStream((* 6
(((6 7
Encoding((7 ?
.((? @
Unicode((@ G
.((G H
GetBytes((H P
(((P Q
json((Q U
)((U V
)((V W
)((W X
{)) 
var** 
serializerSettings** &
=**' (
settings**) 1
??**2 4
new**5 8.
"DataContractJsonSerializerSettings**9 [
{**\ ]%
UseSimpleDictionaryFormat**^ w
=**x y
true**z ~
}	** Ä
;
**Ä Å
var,, 

serializer,, 
=,,  
new,,! $&
DataContractJsonSerializer,,% ?
(,,? @
typeof,,@ F
(,,F G
T,,G H
),,H I
,,,I J
serializerSettings,,K ]
),,] ^
;,,^ _
return-- 
(-- 
T-- 
)-- 

serializer-- $
.--$ %

ReadObject--% /
(--/ 0
memoryStream--0 <
)--< =
;--= >
}.. 
}// 	
}00 
}11 ‹€
VD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\OutboundPlugin.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
{ 
public 

class 
OutboundPlugin 
:  !
IPlugin" )
{ 
[ 	
System	 
. 
Diagnostics 
. 
CodeAnalysis (
.( )
SuppressMessage) 8
(8 9
$str 
, 
$str  D
,D E
Justification 
= 
$str @
)@ A
]A B
private 
const 
string 
DefaultBaseUrl +
=, -
$str. Q
;Q R
private 
const 
string 
DefaultFrom (
=) *
$str+ 5
;5 6
private 
const 
string 
AccountEntityName .
=/ 0
$str1 W
;W X
private 
static 
readonly 

HttpClient  *
SharedHttpClient+ ;
=< =
new> A

HttpClientB L
(L M
)M N
;N O
private 
readonly 

HttpClient #

httpClient$ .
;. /
public 
OutboundPlugin 
( 
) 
{ 	
this 
. 

httpClient 
= 
SharedHttpClient .
;. /
} 	
public"" 
OutboundPlugin"" 
("" 
HttpMessageHandler"" 0
httpMessageHandler""1 C
)""C D
{## 	
this$$ 
.$$ 

httpClient$$ 
=$$ 
new$$ !

HttpClient$$" ,
($$, -
httpMessageHandler$$- ?
)$$? @
;$$@ A
}%% 	
public'' 
void'' 
Execute'' 
('' 
IServiceProvider'' ,
serviceProvider''- <
)''< =
{(( 	
var)) 
tracingService)) 
=))  
serviceProvider))! 0
.))0 1
Get))1 4
<))4 5
ITracingService))5 D
>))D E
())E F
)))F G
;))G H
tracingService** 
.** 
Trace**  
(**  !
$str**! H
)**H I
;**I J
var++ "
pluginExecutionContext++ &
=++' (
serviceProvider++) 8
.++8 9
Get++9 <
<++< =#
IPluginExecutionContext++= T
>++T U
(++U V
)++V W
;++W X
var-- 
payload-- 
=-- "
pluginExecutionContext-- 0
.--0 1
InputParameters--1 @
[--@ A
$str--A J
]--J K
as--L N
string--O U
;--U V
tracingService.. 
... 
Trace..  
(..  !
payload..! (
)..( )
;..) *
var00 
payloadObject00 
=00 
	JsonUtils00  )
.00) *
Deserialize00* 5
<005 6
Payload006 =
>00= >
(00> ?
payload00? F
)00F G
;00G H
var22 
status22 
=22 
$str22 !
;22! "
var33 
msgGuid33 
=33 
string33  
.33  !
IsNullOrEmpty33! .
(33. /
payloadObject33/ <
.33< =
	RequestId33= F
)33F G
?33H I
Guid33J N
.33N O
NewGuid33O V
(33V W
)33W X
:33Y Z
Guid33[ _
.33_ `
Parse33` e
(33e f
payloadObject33f s
.33s t
	RequestId33t }
)33} ~
;33~ 
var44 
	messageId44 
=44 
msgGuid44 #
.44# $
ToString44$ ,
(44, -
)44- .
;44. /
var55 
statusDetails55 
=55 
new55  #

Dictionary55$ .
<55. /
string55/ 5
,555 6
object557 =
>55= >
(55> ?
)55? @
;55@ A
try77 
{88 
var99 
rawFrom99 
=99 
payloadObject99 +
.99+ ,
From99, 0
??991 3
DefaultFrom994 ?
;99? @
tracingService:: 
.:: 
Trace:: $
(::$ %
$"::% '
$str::' 1
{::1 2
rawFrom::2 9
}::9 :
$str::: D
{::D E
payloadObject::E R
.::R S
ChannelInstanceId::S d
}::d e
"::e f
)::f g
;::g h
var<< 

orgService<< 
=<<  
serviceProvider<<! 0
.<<0 1
Get<<1 4
<<<4 5'
IOrganizationServiceFactory<<5 P
><<P Q
(<<Q R
)<<R S
.<<S T%
CreateOrganizationService<<T m
(<<m n
null<<n r
)<<r s
;<<s t
var>> 
accountEntity>> !
=>>" # 
ResolveAccountEntity>>$ 8
(>>8 9

orgService>>9 C
,>>C D
payloadObject>>E R
,>>R S
rawFrom>>T [
,>>[ \
tracingService>>] k
)>>k l
;>>l m
ValidateAccount?? 
(??  
accountEntity??  -
,??- .
rawFrom??/ 6
)??6 7
;??7 8
varAA 
appIdAA 
=AA 
accountEntityAA )
.AA) *
GetAttributeValueAA* ;
<AA; <
stringAA< B
>AAB C
(AAC D
$strAAD U
)AAU V
;AAV W
varBB 
	appSecretBB 
=BB 
accountEntityBB  -
.BB- .
GetAttributeValueBB. ?
<BB? @
stringBB@ F
>BBF G
(BBG H
$strBBH ]
)BB] ^
;BB^ _
varCC 
baseUrlCC 
=CC 
ResolveBaseUrlCC ,
(CC, -
accountEntityCC- :
)CC: ;
;CC; <
varEE 
superExternalIdEE #
=EE$ %
BuildExternalIdEE& 5
(EE5 6
msgGuidEE6 =
,EE= >
rawFromEE? F
)EEF G
;EEG H
varFF 
accessTokenFF 
=FF  !
AcquireTokenFF" .
(FF. /
baseUrlFF/ 6
,FF6 7
appIdFF8 =
,FF= >
	appSecretFF? H
)FFH I
;FFI J
	messageIdHH 
=HH 
SendSmsHH #
(HH# $
baseUrlHH$ +
,HH+ ,
accessTokenHH- 8
,HH8 9
payloadObjectHH: G
,HHG H
superExternalIdHHI X
,HHX Y
tracingServiceHHZ h
)HHh i
;HHi j
statusII 
=II 
$strII 
;II  
}JJ 
catchKK 
(KK 
	ExceptionKK 
exKK 
)KK  
{LL 
statusNN 
=NN 
$strNN !
;NN! "
statusDetailsOO 
.OO 
AddOO !
(OO! "
$strOO" 0
,OO0 1
exOO2 4
.OO4 5
MessageOO5 <
)OO< =
;OO= >
tracingServicePP 
.PP 
TracePP $
(PP$ %
$strPP% 8
+PP9 :
exPP; =
.PP= >
ToStringPP> F
(PPF G
)PPG H
)PPH I
;PPI J
}QQ 
varSS 
responseObjectSS 
=SS  
newSS! $
ResponseSS% -
(SS- .
)SS. /
{TT 
ChannelDefinitionIdUU #
=UU$ %
payloadObjectUU& 3
.UU3 4
ChannelDefinitionIdUU4 G
,UUG H
	MessageIdVV 
=VV 
	messageIdVV %
,VV% &
	RequestIdWW 
=WW 
payloadObjectWW )
.WW) *
	RequestIdWW* 3
,WW3 4
StatusXX 
=XX 
statusXX 
,XX  
StatusDetailsYY 
=YY 
statusDetailsYY  -
.YY- .
CountYY. 3
>YY4 5
$numYY6 7
?YY8 9
statusDetailsYY: G
:YYH I
nullYYJ N
}ZZ 
;ZZ "
pluginExecutionContext\\ "
.\\" #
OutputParameters\\# 3
[\\3 4
$str\\4 >
]\\> ?
=\\@ A
	JsonUtils\\B K
.\\K L
	Serialize\\L U
(\\U V
responseObject\\V d
)\\d e
;\\e f
}]] 	
private`` 
static`` 
Entity``  
ResolveAccountEntity`` 2
(``2 3 
IOrganizationService``3 G

orgService``H R
,``R S
Payload``T [
payloadObject``\ i
,``i j
string``k q
rawFrom``r y
,``y z
ITracingService	``{ ä
tracingService
``ã ô
)
``ô ö
{aa 	
Entitybb 
accountEntitybb  
=bb! "
nullbb# '
;bb' (
ifdd 
(dd  
IsConcreteInstanceIddd $
(dd$ %
payloadObjectdd% 2
.dd2 3
ChannelInstanceIddd3 D
)ddD E
)ddE F
{ee 
tracingServiceff 
.ff 
Traceff $
(ff$ %
$strff% A
)ffA B
;ffB C
trygg 
{hh 
accountEntityii !
=ii" #

orgServiceii$ .
.ii. /
Retrieveii/ 7
(ii7 8
AccountEntityNamejj )
,jj) *
Guidkk 
.kk 
Parsekk "
(kk" #
payloadObjectkk# 0
.kk0 1
ChannelInstanceIdkk1 B
)kkB C
,kkC D
newll 
	ColumnSetll %
(ll% &
$strll& 7
,ll7 8
$strll9 N
,llN O
$strllP b
,llb c
$strlld o
)llo p
)llp q
;llq r
}mm 
catchnn 
(nn 
	Exceptionnn  
exnn! #
)nn# $
{oo 
tracingServiceqq "
.qq" #
Traceqq# (
(qq( )
$strqq) 9
+qq: ;
exqq< >
.qq> ?
Messageqq? F
)qqF G
;qqG H
}rr 
}ss 
returnuu 
accountEntityuu  
??uu! #&
QueryAccountByContactPointuu$ >
(uu> ?

orgServiceuu? I
,uuI J
rawFromuuK R
,uuR S
tracingServiceuuT b
)uub c
;uuc d
}vv 	
privatexx 
staticxx 
boolxx  
IsConcreteInstanceIdxx 0
(xx0 1
stringxx1 7
channelInstanceIdxx8 I
)xxI J
{yy 	
returnzz 
!zz 
stringzz 
.zz 
IsNullOrEmptyzz (
(zz( )
channelInstanceIdzz) :
)zz: ;
&&{{ 
channelInstanceId{{ $
!={{% '
Guid{{( ,
.{{, -
Empty{{- 2
.{{2 3
ToString{{3 ;
({{; <
){{< =
&&|| 
channelInstanceId|| $
.||$ %
Replace||% ,
(||, -
$str||- 0
,||0 1
$str||2 4
)||4 5
!=||6 8
$str||9 [
;||[ \
}}} 	
private
ÄÄ 
static
ÄÄ 
Entity
ÄÄ (
QueryAccountByContactPoint
ÄÄ 8
(
ÄÄ8 9"
IOrganizationService
ÄÄ9 M

orgService
ÄÄN X
,
ÄÄX Y
string
ÄÄZ `
rawFrom
ÄÄa h
,
ÄÄh i
ITracingService
ÄÄj y
tracingServiceÄÄz à
)ÄÄà â
{
ÅÅ 	
tracingService
ÇÇ 
.
ÇÇ 
Trace
ÇÇ  
(
ÇÇ  !
$"
ÇÇ! #
$str
ÇÇ# =
{
ÇÇ= >
rawFrom
ÇÇ> E
}
ÇÇE F
"
ÇÇF G
)
ÇÇG H
;
ÇÇH I
var
ÑÑ 
fallbackQuery
ÑÑ 
=
ÑÑ 
new
ÑÑ  #
QueryExpression
ÑÑ$ 3
(
ÑÑ3 4
AccountEntityName
ÑÑ4 E
)
ÑÑE F
{
ÖÖ 
TopCount
ÜÜ 
=
ÜÜ 
$num
ÜÜ 
,
ÜÜ 
	ColumnSet
áá 
=
áá 
new
áá 
	ColumnSet
áá  )
(
áá) *
$str
áá* ;
,
áá; <
$str
áá= R
,
ááR S
$str
ááT f
,
ááf g
$str
ááh s
)
áás t
}
àà 
;
àà 
fallbackQuery
ââ 
.
ââ 
Criteria
ââ "
.
ââ" #
AddCondition
ââ# /
(
ââ/ 0
$str
ââ0 ;
,
ââ; <
ConditionOperator
ââ= N
.
ââN O
Equal
ââO T
,
ââT U
$num
ââV W
)
ââW X
;
ââX Y
var
ãã 
accountLink
ãã 
=
ãã 
fallbackQuery
ãã +
.
ãã+ ,
AddLink
ãã, 3
(
ãã3 4
$str
åå .
,
åå. /
$str
çç 8
,
çç8 9
$str
éé (
,
éé( )
JoinOperator
èè 
.
èè 
Inner
èè "
)
èè" #
;
èè# $
var
ëë 
instanceLink
ëë 
=
ëë 
accountLink
ëë *
.
ëë* +
AddLink
ëë+ 2
(
ëë2 3
$str
íí '
,
íí' (
$str
ìì 0
,
ìì0 1
$str
îî 0
,
îî0 1
JoinOperator
ïï 
.
ïï 
Inner
ïï "
)
ïï" #
;
ïï# $
instanceLink
óó 
.
óó 
LinkCriteria
óó %
.
óó% &
AddCondition
óó& 2
(
óó2 3
$str
óó3 G
,
óóG H
ConditionOperator
óóI Z
.
óóZ [
Equal
óó[ `
,
óó` a
rawFrom
óób i
)
óói j
;
óój k
var
ôô 
fallbackResults
ôô 
=
ôô  !

orgService
ôô" ,
.
ôô, -
RetrieveMultiple
ôô- =
(
ôô= >
fallbackQuery
ôô> K
)
ôôK L
;
ôôL M
if
öö 
(
öö 
fallbackResults
öö 
.
öö  
Entities
öö  (
.
öö( )
Count
öö) .
>
öö/ 0
$num
öö1 2
)
öö2 3
{
õõ 
var
úú 
entity
úú 
=
úú 
fallbackResults
úú ,
.
úú, -
Entities
úú- 5
[
úú5 6
$num
úú6 7
]
úú7 8
;
úú8 9
tracingService
ùù 
.
ùù 
Trace
ùù $
(
ùù$ %
$"
ùù% '
$str
ùù' 3
{
ùù3 4
entity
ùù4 :
.
ùù: ;
Id
ùù; =
}
ùù= >
$str
ùù> J
{
ùùJ K
entity
ùùK Q
.
ùùQ R
GetAttributeValue
ùùR c
<
ùùc d
string
ùùd j
>
ùùj k
(
ùùk l
$str
ùùl }
)
ùù} ~
}
ùù~ 
$strùù å
{ùùå ç
entityùùç ì
.ùùì î!
GetAttributeValueùùî •
<ùù• ¶
stringùù¶ ¨
>ùù¨ ≠
(ùù≠ Æ
$strùùÆ ¿
)ùù¿ ¡
}ùù¡ ¬
"ùù¬ √
)ùù√ ƒ
;ùùƒ ≈
return
ûû 
entity
ûû 
;
ûû 
}
üü 
return
°° 
null
°° 
;
°° 
}
¢¢ 	
private
§§ 
static
§§ 
void
§§ 
ValidateAccount
§§ +
(
§§+ ,
Entity
§§, 2
accountEntity
§§3 @
,
§§@ A
string
§§B H
rawFrom
§§I P
)
§§P Q
{
•• 	
if
¶¶ 
(
¶¶ 
accountEntity
¶¶ 
==
¶¶  
null
¶¶! %
)
¶¶% &
{
ßß 
throw
®® 
new
®® #
XgateGatewayException
®® /
(
®®/ 0
$"
®®0 2
$str
®®2 D
{
®®D E
rawFrom
®®E L
}
®®L M
$str
®®M m
"
®®m n
)
®®n o
;
®®o p
}
©© 
var
´´ 
	stateCode
´´ 
=
´´ 
accountEntity
´´ )
.
´´) *
GetAttributeValue
´´* ;
<
´´; <
OptionSetValue
´´< J
>
´´J K
(
´´K L
$str
´´L W
)
´´W X
;
´´X Y
if
¨¨ 
(
¨¨ 
	stateCode
¨¨ 
!=
¨¨ 
null
¨¨ !
&&
¨¨" $
	stateCode
¨¨% .
.
¨¨. /
Value
¨¨/ 4
!=
¨¨5 7
$num
¨¨8 9
)
¨¨9 :
{
≠≠ 
throw
ÆÆ 
new
ÆÆ #
XgateGatewayException
ÆÆ /
(
ÆÆ/ 0
$str
ÆÆ0 J
)
ÆÆJ K
;
ÆÆK L
}
ØØ 
}
∞∞ 	
private
≤≤ 
static
≤≤ 
string
≤≤ 
ResolveBaseUrl
≤≤ ,
(
≤≤, -
Entity
≤≤- 3
accountEntity
≤≤4 A
)
≤≤A B
{
≥≥ 	
var
¥¥ 
baseUrl
¥¥ 
=
¥¥ 
accountEntity
¥¥ '
.
¥¥' (
GetAttributeValue
¥¥( 9
<
¥¥9 :
string
¥¥: @
>
¥¥@ A
(
¥¥A B
$str
¥¥B T
)
¥¥T U
;
¥¥U V
if
µµ 
(
µµ 
string
µµ 
.
µµ  
IsNullOrWhiteSpace
µµ )
(
µµ) *
baseUrl
µµ* 1
)
µµ1 2
)
µµ2 3
{
∂∂ 
baseUrl
∑∑ 
=
∑∑ 
DefaultBaseUrl
∑∑ (
;
∑∑( )
}
∏∏ 
return
∫∫ 
baseUrl
∫∫ 
.
∫∫ 
TrimEnd
∫∫ "
(
∫∫" #
$char
∫∫# &
)
∫∫& '
;
∫∫' (
}
ªª 	
private
ææ 
static
ææ 
string
ææ 
BuildExternalId
ææ -
(
ææ- .
Guid
ææ. 2
msgGuid
ææ3 :
,
ææ: ;
string
ææ< B
rawFrom
ææC J
)
ææJ K
{
øø 	
var
¿¿ 
safeFromNumber
¿¿ 
=
¿¿  
Uri
¿¿! $
.
¿¿$ %
EscapeDataString
¿¿% 5
(
¿¿5 6
rawFrom
¿¿6 =
)
¿¿= >
;
¿¿> ?
var
¡¡ 
msgB64
¡¡ 
=
¡¡ 
Convert
¡¡  
.
¡¡  !
ToBase64String
¡¡! /
(
¡¡/ 0
msgGuid
¡¡0 7
.
¡¡7 8
ToByteArray
¡¡8 C
(
¡¡C D
)
¡¡D E
)
¡¡E F
.
¡¡F G
Replace
¡¡G N
(
¡¡N O
$str
¡¡O R
,
¡¡R S
$str
¡¡T W
)
¡¡W X
.
¡¡X Y
Replace
¡¡Y `
(
¡¡` a
$str
¡¡a d
,
¡¡d e
$str
¡¡f i
)
¡¡i j
.
¡¡j k
TrimEnd
¡¡k r
(
¡¡r s
$char
¡¡s v
)
¡¡v w
;
¡¡w x
return
¬¬ 
$"
¬¬ 
{
¬¬ 
msgB64
¬¬ 
}
¬¬ 
$str
¬¬ 
{
¬¬ 
safeFromNumber
¬¬ -
}
¬¬- .
"
¬¬. /
;
¬¬/ 0
}
√√ 	
private
≈≈ 
string
≈≈ 
AcquireToken
≈≈ #
(
≈≈# $
string
≈≈$ *
baseUrl
≈≈+ 2
,
≈≈2 3
string
≈≈4 :
appId
≈≈; @
,
≈≈@ A
string
≈≈B H
	appSecret
≈≈I R
)
≈≈R S
{
∆∆ 	
var
«« 
tokenRequestBody
««  
=
««! "
	JsonUtils
««# ,
.
««, -
	Serialize
««- 6
(
««6 7
new
««7 :
TokenRequest
««; G
{
««H I
AppId
««J O
=
««P Q
appId
««R W
,
««W X
	AppSecret
««Y b
=
««c d
	appSecret
««e n
}
««o p
)
««p q
;
««q r
var
»» 
tokenHttpResponse
»» !
=
»»" #

httpClient
»»$ .
.
»». /
	PostAsync
»»/ 8
(
»»8 9
$"
…… 
{
…… 
baseUrl
…… 
}
…… 
$str
…… !
"
……! "
,
……" #
new
   
StringContent
   !
(
  ! "
tokenRequestBody
  " 2
,
  2 3
Encoding
  4 <
.
  < =
UTF8
  = A
,
  A B
$str
  C U
)
  U V
)
  V W
.
  W X

GetAwaiter
  X b
(
  b c
)
  c d
.
  d e
	GetResult
  e n
(
  n o
)
  o p
;
  p q
var
ÃÃ 
tokenResponseBody
ÃÃ !
=
ÃÃ" #
tokenHttpResponse
ÃÃ$ 5
.
ÃÃ5 6
Content
ÃÃ6 =
.
ÃÃ= >
ReadAsStringAsync
ÃÃ> O
(
ÃÃO P
)
ÃÃP Q
.
ÃÃQ R

GetAwaiter
ÃÃR \
(
ÃÃ\ ]
)
ÃÃ] ^
.
ÃÃ^ _
	GetResult
ÃÃ_ h
(
ÃÃh i
)
ÃÃi j
;
ÃÃj k
if
ÕÕ 
(
ÕÕ 
!
ÕÕ 
tokenHttpResponse
ÕÕ "
.
ÕÕ" #!
IsSuccessStatusCode
ÕÕ# 6
)
ÕÕ6 7
{
ŒŒ 
throw
œœ 
new
œœ #
XgateGatewayException
œœ /
(
œœ/ 0
$"
œœ0 2
$str
œœ2 ?
{
œœ? @
tokenHttpResponse
œœ@ Q
.
œœQ R

StatusCode
œœR \
}
œœ\ ]
$str
œœ] `
{
œœ` a
tokenResponseBody
œœa r
}
œœr s
"
œœs t
)
œœt u
;
œœu v
}
–– 
return
““ 
	JsonUtils
““ 
.
““ 
Deserialize
““ (
<
““( )
TokenResponse
““) 6
>
““6 7
(
““7 8
tokenResponseBody
““8 I
)
““I J
.
““J K
AccessToken
““K V
;
““V W
}
”” 	
private
’’ 
string
’’ 
SendSms
’’ 
(
’’ 
string
’’ %
baseUrl
’’& -
,
’’- .
string
’’/ 5
accessToken
’’6 A
,
’’A B
Payload
’’C J
payloadObject
’’K X
,
’’X Y
string
’’Z `
superExternalId
’’a p
,
’’p q
ITracingService’’r Å
tracingService’’Ç ê
)’’ê ë
{
÷÷ 	
var
◊◊ 
smsRequestBody
◊◊ 
=
◊◊  
	JsonUtils
◊◊! *
.
◊◊* +
	Serialize
◊◊+ 4
(
◊◊4 5
new
◊◊5 8

SmsRequest
◊◊9 C
{
ÿÿ 
MessageBody
ŸŸ 
=
ŸŸ  
ResolveMessageBody
ŸŸ 0
(
ŸŸ0 1
payloadObject
ŸŸ1 >
)
ŸŸ> ?
,
ŸŸ? @
ToList
⁄⁄ 
=
⁄⁄ 
new
⁄⁄ 
List
⁄⁄ !
<
⁄⁄! "
SmsRecipient
⁄⁄" .
>
⁄⁄. /
{
€€ 
new
‹‹ 
SmsRecipient
‹‹ $
{
‹‹% &
To
‹‹' )
=
‹‹* +
payloadObject
‹‹, 9
.
‹‹9 :
To
‹‹: <
,
‹‹< =

ExternalId
‹‹> H
=
‹‹I J
superExternalId
‹‹K Z
}
‹‹[ \
}
›› 
}
ﬁﬁ 
)
ﬁﬁ 
;
ﬁﬁ 
var
‡‡ 
smsHttpRequest
‡‡ 
=
‡‡  
new
‡‡! $ 
HttpRequestMessage
‡‡% 7
(
‡‡7 8

HttpMethod
‡‡8 B
.
‡‡B C
Post
‡‡C G
,
‡‡G H
$"
‡‡I K
{
‡‡K L
baseUrl
‡‡L S
}
‡‡S T
$str
‡‡T Y
"
‡‡Y Z
)
‡‡Z [
{
·· 
Content
‚‚ 
=
‚‚ 
new
‚‚ 
StringContent
‚‚ +
(
‚‚+ ,
smsRequestBody
‚‚, :
,
‚‚: ;
Encoding
‚‚< D
.
‚‚D E
UTF8
‚‚E I
,
‚‚I J
$str
‚‚K ]
)
‚‚] ^
}
„„ 
;
„„ 
smsHttpRequest
‰‰ 
.
‰‰ 
Headers
‰‰ "
.
‰‰" #
Authorization
‰‰# 0
=
‰‰1 2
new
‰‰3 6
System
‰‰7 =
.
‰‰= >
Net
‰‰> A
.
‰‰A B
Http
‰‰B F
.
‰‰F G
Headers
‰‰G N
.
‰‰N O'
AuthenticationHeaderValue
‰‰O h
(
‰‰h i
$str
‰‰i q
,
‰‰q r
accessToken
‰‰s ~
)
‰‰~ 
;‰‰ Ä
var
ÊÊ 
smsHttpResponse
ÊÊ 
=
ÊÊ  !

httpClient
ÊÊ" ,
.
ÊÊ, -
	SendAsync
ÊÊ- 6
(
ÊÊ6 7
smsHttpRequest
ÊÊ7 E
)
ÊÊE F
.
ÊÊF G

GetAwaiter
ÊÊG Q
(
ÊÊQ R
)
ÊÊR S
.
ÊÊS T
	GetResult
ÊÊT ]
(
ÊÊ] ^
)
ÊÊ^ _
;
ÊÊ_ `
var
ÁÁ 
smsResponseBody
ÁÁ 
=
ÁÁ  !
smsHttpResponse
ÁÁ" 1
.
ÁÁ1 2
Content
ÁÁ2 9
.
ÁÁ9 :
ReadAsStringAsync
ÁÁ: K
(
ÁÁK L
)
ÁÁL M
.
ÁÁM N

GetAwaiter
ÁÁN X
(
ÁÁX Y
)
ÁÁY Z
.
ÁÁZ [
	GetResult
ÁÁ[ d
(
ÁÁd e
)
ÁÁe f
;
ÁÁf g
if
ÍÍ 
(
ÍÍ 
!
ÍÍ 
smsHttpResponse
ÍÍ  
.
ÍÍ  !!
IsSuccessStatusCode
ÍÍ! 4
)
ÍÍ4 5
{
ÎÎ 
throw
ÏÏ 
new
ÏÏ #
XgateGatewayException
ÏÏ /
(
ÏÏ/ 0
$"
ÏÏ0 2
$str
ÏÏ2 A
{
ÏÏA B
smsHttpResponse
ÏÏB Q
.
ÏÏQ R

StatusCode
ÏÏR \
}
ÏÏ\ ]
$str
ÏÏ] `
{
ÏÏ` a
smsResponseBody
ÏÏa p
}
ÏÏp q
"
ÏÏq r
)
ÏÏr s
;
ÏÏs t
}
ÌÌ 
var
ÔÔ 
smsResponse
ÔÔ 
=
ÔÔ 
	JsonUtils
ÔÔ '
.
ÔÔ' (
Deserialize
ÔÔ( 3
<
ÔÔ3 4
SmsResponse
ÔÔ4 ?
>
ÔÔ? @
(
ÔÔ@ A
smsResponseBody
ÔÔA P
)
ÔÔP Q
;
ÔÔQ R
if
ÚÚ 
(
ÚÚ 
smsResponse
ÚÚ 
.
ÚÚ 
CountOfStatus
ÚÚ )
!=
ÚÚ* ,
null
ÚÚ- 1
&&
ÚÚ2 4
smsResponse
ÚÚ5 @
.
ÚÚ@ A
CountOfStatus
ÚÚA N
.
ÚÚN O
Success
ÚÚO V
==
ÚÚW Y
$num
ÚÚZ [
&&
ÛÛ 
smsResponse
ÛÛ 
.
ÛÛ 
ReceiveInfo
ÛÛ *
!=
ÛÛ+ -
null
ÛÛ. 2
&&
ÛÛ3 5
smsResponse
ÛÛ6 A
.
ÛÛA B
ReceiveInfo
ÛÛB M
.
ÛÛM N
Count
ÛÛN S
>
ÛÛT U
$num
ÛÛV W
)
ÛÛW X
{
ÙÙ 
var
ıı 
	messageId
ıı 
=
ıı 
smsResponse
ıı  +
.
ıı+ ,
ReceiveInfo
ıı, 7
[
ıı7 8
$num
ıı8 9
]
ıı9 :
.
ıı: ;
	MessageId
ıı; D
;
ııD E
tracingService
ˆˆ 
.
ˆˆ 
Trace
ˆˆ $
(
ˆˆ$ %
$str
ˆˆ% I
+
ˆˆJ K
	messageId
ˆˆL U
)
ˆˆU V
;
ˆˆV W
return
˜˜ 
	messageId
˜˜  
;
˜˜  !
}
¯¯ 
throw
˙˙ 
new
˙˙ #
XgateGatewayException
˙˙ +
(
˙˙+ ,
$"
˙˙, .
$str
˙˙. =
{
˙˙= >
smsResponseBody
˙˙> M
}
˙˙M N
"
˙˙N O
)
˙˙O P
;
˙˙P Q
}
˚˚ 	
private
˝˝ 
static
˝˝ 
string
˝˝  
ResolveMessageBody
˝˝ 0
(
˝˝0 1
Payload
˝˝1 8
payloadObject
˝˝9 F
)
˝˝F G
{
˛˛ 	
return
ˇˇ 
payloadObject
ˇˇ  
.
ˇˇ  !
Message
ˇˇ! (
.
ˇˇ( )
ContainsKey
ˇˇ) 4
(
ˇˇ4 5
$str
ˇˇ5 ;
)
ˇˇ; <
?
ÄÄ 
payloadObject
ÄÄ 
.
ÄÄ  
Message
ÄÄ  '
[
ÄÄ' (
$str
ÄÄ( .
]
ÄÄ. /
:
ÅÅ 
payloadObject
ÅÅ 
.
ÅÅ  
Message
ÅÅ  '
.
ÅÅ' (
Values
ÅÅ( .
.
ÅÅ. /
FirstOrDefault
ÅÅ/ =
(
ÅÅ= >
)
ÅÅ> ?
??
ÅÅ@ B
string
ÅÅC I
.
ÅÅI J
Empty
ÅÅJ O
;
ÅÅO P
}
ÇÇ 	
}
ÉÉ 
}ÑÑ œ	
pD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\ProviderContracts\ProviderDeliveryReport.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
{ 
[ 
DataContract 
] 
public 

class "
ProviderDeliveryReport '
{ 
[ 	

DataMember	 
] 
public		 
string		 

MessageSid		  
{		! "
get		# &
;		& '
set		( +
;		+ ,
}		- .
[ 	

DataMember	 
] 
public 
string 
From 
{ 
get  
;  !
set" %
;% &
}' (
[ 	

DataMember	 
] 
public 
string 
MessageStatus #
{$ %
get& )
;) *
set+ .
;. /
}0 1
[ 	

DataMember	 
] 
public 
string 
	RequestId 
{  !
get" %
;% &
set' *
;* +
}, -
} 
} ‹
gD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\XgateContracts\SmsCountOfStatus.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
.! "
XgateContracts" 0
{ 
[ 
DataContract 
] 
public 

class 
SmsCountOfStatus !
{ 
[ 	

DataMember	 
( 
Name 
= 
$str $
)$ %
]% &
public		 
int		 
Success		 
{		 
get		  
;		  !
set		" %
;		% &
}		' (
[ 	

DataMember	 
( 
Name 
= 
$str #
)# $
]$ %
public 
int 
Failed 
{ 
get 
;  
set! $
;$ %
}& '
} 
} ø
eD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\XgateContracts\SmsReceiveInfo.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
.! "
XgateContracts" 0
{ 
[ 
DataContract 
] 
public 

class 
SmsReceiveInfo 
{ 
[ 	

DataMember	 
( 
Name 
= 
$str &
)& '
]' (
public		 
string		 
	MessageId		 
{		  !
get		" %
;		% &
set		' *
;		* +
}		, -
}

 
} Ÿ
cD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\XgateContracts\SmsRecipient.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
.! "
XgateContracts" 0
{ 
[ 
DataContract 
] 
public 

class 
SmsRecipient 
{ 
[ 	

DataMember	 
( 
Name 
= 
$str 
)  
]  !
public		 
string		 
To		 
{		 
get		 
;		 
set		  #
;		# $
}		% &
[ 	

DataMember	 
( 
Name 
= 
$str '
)' (
]( )
public 
string 

ExternalId  
{! "
get# &
;& '
set( +
;+ ,
}- .
} 
} ê
aD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\XgateContracts\SmsRequest.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
.! "
XgateContracts" 0
{ 
[ 
DataContract 
] 
public 

class 

SmsRequest 
{ 
[		 	

DataMember			 
(		 
Name		 
=		 
$str		 (
)		( )
]		) *
public

 
string

 
MessageBody

 !
{

" #
get

$ '
;

' (
set

) ,
;

, -
}

. /
[ 	

DataMember	 
( 
Name 
= 
$str #
)# $
]$ %
public 
List 
< 
SmsRecipient  
>  !
ToList" (
{) *
get+ .
;. /
set0 3
;3 4
}5 6
} 
} •
bD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\XgateContracts\SmsResponse.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
.! "
XgateContracts" 0
{ 
[ 
DataContract 
] 
public 

class 
SmsResponse 
{ 
[		 	

DataMember			 
(		 
Name		 
=		 
$str		 *
)		* +
]		+ ,
public

 
SmsCountOfStatus

 
CountOfStatus

  -
{

. /
get

0 3
;

3 4
set

5 8
;

8 9
}

: ;
[ 	

DataMember	 
( 
Name 
= 
$str (
)( )
]) *
public 
List 
< 
SmsReceiveInfo "
>" #
ReceiveInfo$ /
{0 1
get2 5
;5 6
set7 :
;: ;
}< =
} 
} €
cD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\XgateContracts\TokenRequest.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
.! "
XgateContracts" 0
{ 
[ 
DataContract 
] 
public 

class 
TokenRequest 
{ 
[ 	

DataMember	 
( 
Name 
= 
$str "
)" #
]# $
public		 
string		 
AppId		 
{		 
get		 !
;		! "
set		# &
;		& '
}		( )
[ 	

DataMember	 
( 
Name 
= 
$str &
)& '
]' (
public 
string 
	AppSecret 
{  !
get" %
;% &
set' *
;* +
}, -
} 
} ø
dD:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\XgateContracts\TokenResponse.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
.! "
XgateContracts" 0
{ 
[ 
DataContract 
] 
public 

class 
TokenResponse 
{ 
[ 	

DataMember	 
( 
Name 
= 
$str (
)( )
]) *
public		 
string		 
AccessToken		 !
{		" #
get		$ '
;		' (
set		) ,
;		, -
}		. /
}

 
} ï
]D:\work\Dynamics365-Solution\XgateSmsChannel\XgateSmsChannel.Plugins\XgateGatewayException.cs
	namespace 	
XgateSmsChannel
 
. 
Plugins !
{ 
[		 
Serializable		 
]		 
public

 

class

 !
XgateGatewayException

 &
:

' (
	Exception

) 2
{ 
public !
XgateGatewayException $
($ %
)% &
{ 	
} 	
public !
XgateGatewayException $
($ %
string% +
message, 3
)3 4
: 
base 
( 
message 
) 
{ 	
} 	
public !
XgateGatewayException $
($ %
string% +
message, 3
,3 4
	Exception5 >
innerException? M
)M N
: 
base 
( 
message 
, 
innerException *
)* +
{ 	
} 	
	protected !
XgateGatewayException '
(' (
System 
. 
Runtime 
. 
Serialization (
.( )
SerializationInfo) :
info; ?
,? @
System 
. 
Runtime 
. 
Serialization (
.( )
StreamingContext) 9
context: A
)A B
: 
base 
( 
info 
, 
context  
)  !
{ 	
} 	
}   
}!! 