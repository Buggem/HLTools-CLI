;$singledest     valve/gfx
$dest  valve/cached.wad

;$loadbmp valve/gfx/palette.bmp
;palette                palette
;colormap       colormap        32              32      ; 32 levels, 16 fullbright colors
;colormap       colormap2       2       64              32      ; 200% intensity, 64 levels

$loadbmp valve/gfx/menu/qlogo.bmp
pop     raw      16 16 16 16

$loadbmp valve/gfx/menu/inter01.bmp
complete        qpic    63              24              192     24
inter   qpic    0               56              160     144

$loadbmp valve/gfx/menu/options2.bmp
ranking         qpic    16      80      168     24
vidmodes        qpic    16      112     216     24

$loadbmp valve/gfx/menu/finale.bmp
finale  qpic    0               0       288     24

$loadbmp valve/gfx/status/logo.bmp
conback         qpic    0               0               320     200

;$loadbmp valve/gfx/menu/menu06.bmp
qplaque         qpic            16      16      32      144
;mainmenu       qpic    80 16 304 144

menudot1        qpic    72 176 16 24
menudot2        qpic    96 176 16 24
menudot3        qpic    120 176 16 24
menudot4        qpic    144 176 16 24
menudot5        qpic    168 176 16 24
menudot6        qpic    192 176 16 24

$loadbmp valve/gfx/menu/player1.bmp
menuplyr        qpic    16      16      48      56
bigbox          qpic    144     16      72      72

$loadbmp valve/gfx/menu/menu14.bmp
dim_modm                qpic    80      16      176     19
dim_drct                qpic    80      35      176     19
dim_ipx                 qpic    80      54      176     19
dim_tcp                 qpic    80      73      176     19
dim_mult                qpic    80      92      176     19

$loadbmp valve/gfx/menu/menu10.bmp
mainmenu        qpic    80      16      240     112

; little box to print text into
; top left, middle, right
box_tl          qpic    208     128     8       8
box_tm          qpic    216     128     16      8
box_tr          qpic    232     128     8       8

; middle left, middle, right
box_ml          qpic    208     136     8       8
box_mm          qpic    216     136     16      8
box_mm2         qpic    56      16      16      8
box_mr          qpic    232     136     8       8

; bottom left, middle, right
box_bl          qpic    208     144     8       8
box_bm          qpic    216     144     16      8
box_br          qpic    232     144     8       8

$loadbmp valve/gfx/menu/menu11.bmp
sp_menu         qpic    80      16      232     64
ttl_sgl         qpic    80      88      128     24
ttl_main        qpic    80      120     96      24
ttl_cstm        qpic    80      152     184     24

$loadbmp valve/gfx/menu/menu12.bmp
mp_menu qpic            80      16      232     64

$loadbmp valve/gfx/menu/menu13.bmp
netmen1         qpic    80      16      176     19
netmen2         qpic    80      35      176     19
netmen3         qpic    80      54      176     19
netmen4         qpic    80      73      176     19
netmen5         qpic    80      92      176     19

$loadbmp valve/gfx/menu/sell.bmp
sell    QPIC    0       0       320     200

$loadbmp valve/gfx/menu/order.bmp
help0   QPIC    0       0       320     200
$loadbmp valve/gfx/menu/help05.bmp
help1   QPIC    0       0       320     200
$loadbmp valve/gfx/menu/help06.bmp
help2   QPIC    0       0       320     200
$loadbmp valve/gfx/menu/help07.bmp
help3   QPIC    0       0       320     200
$loadbmp valve/gfx/menu/help08.bmp
help4   QPIC    0       0       320     200
$loadbmp valve/gfx/menu/help09.bmp
help5   QPIC    0       0       320     200

$loadbmp valve/gfx/status/chip.bmp
pause           qpic            104     136     128     24
loading         qpic            104     104     144     24

; Menu plaques
$loadbmp valve/gfx/menu/options1.bmp
p_option        qpic            16      16      144     24
p_load          qpic            16      48      104     24
p_save          qpic            16      80      88      24
p_multi         qpic            16      112     216     24
