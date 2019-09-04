\ sokoban - a maze game in FORTH

\ Copyright (C) 1995,1997,1998,2003,2007,2012,2013,2015 Free Software Foundation, Inc.

\ This file is part of Gforth.

\ Gforth is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation, either version 3
\ of the License, or (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program. If not, see http://www.gnu.org/licenses/.

\ Contest from Rick VanNorman in comp.lang.forth

\ SOKOBAN

\ Sokoban is a visual game of pushing.  You (Soko) are represented by the
\ at-sign "@"  You may move freely through the maze on unoccupied spaces.
\ The dollar-signs "$" are the rocks you have to push. You can only push
\ one rock at a time, and cannot push a rock through a wall "#" or over
\ another rock. The object is to push the rocks to their goals which are
\ indicated by the periods ".".  There are 50 levels, the first of which
\ is shown below.

\ program is ANS FORTH with environmental dependency of case-insensitiv
\ source. Tested with gforth, bigFORTH and pfe

\ I don't like the keyboard interpreting CASE-statement either, but
\ was to lazy to use a table.
\ I could have used blocks as level tables, but as I don't have a good
\ block editor for gforth now, I let it be.

require ibm-font.fs
require term.fs
require input.fs

40 Constant /maze  \ maximal maze line

ROM

Variable mazes   0 mazes !  \ root pointer
Variable >maze   0 >maze !  \ current compiled maze

RAM

Create maze  1 cells allot /maze 25 * allot  \ current maze
Variable soko               \ player position

\ score information

Variable rocks                \ number of rocks left
Variable level#               \ Current level
Variable moves                \ number of moves
Variable score                \ total number of scores

ROM

:m new-maze ( n -- addr ) \ add a new level
    here mazes rot 1 [host] ?DO [target] @ [host] LOOP [target]  !
    0 , 0 , here >maze ! 0 , ;

[host]
: count-$ ( addr u -- n )
    0 rot rot
    over + swap ?DO  I c@ [char] $ = -  LOOP ;
[target]

:m m: ( "string" -- )  \ add a level line (top first!)
    [host] -1 parse [target] tuck 2dup count-$ >maze @ cell - +!
    mem,
    /maze swap - here over bl fill allot
    >maze @ here over cell+ - swap ! ;

: maze-field ( -- addr n )
    maze dup cell+ swap @ chars ;

: .score ( -- )
    ." Lvl: " level# @ 4 .r ."  Scr: " score @ 4 .r cr
    ." Mov: "  moves @ 4 .r ."  Rck: " rocks @ 4 .r cr ;

: .maze ( -- )  \ display maze
    0 0 at-xy  .score
    cr  maze-field over + swap
    DO  I /maze type cr  /maze chars  +LOOP ;

: find-soko ( -- n )
    maze-field 0
    DO  dup I chars + c@ [char] @ =
	IF  drop I  UNLOOP  EXIT  THEN
    LOOP ( true abort" No player in field!" ) ;

: level ( n -- flag )  \ finds level n
    dup level# !
    mazes  swap 0
    ?DO  @  dup 0= IF  drop false  UNLOOP  EXIT  THEN  LOOP
    cell+ dup @ rocks !
    cell+ dup @ cell+ maze swap chars move
    find-soko soko ! true ;

\ now the playing rules as replacement strings

: 'soko ( -- addr ) \ gives player's address
    maze cell+ soko @ chars + ;

: apply-rule? ( addr u offset -- flag )
    'soko 2swap
    \ offset soko-addr addr u
    0 DO
	over c@ over c@ <>
	IF  drop 2drop false  UNLOOP  EXIT  THEN
	>r over chars + r> char+
    LOOP  2drop drop  true ;

: apply-rule! ( addr u offset -- )
    'soko
    2swap
    \ offset soko-addr addr u
    0 DO
	count rot tuck c! rot tuck chars + rot
    LOOP  2drop drop ;

: play-rule ( addr1 u1 addr2 u2 offset -- flag )
    >r 2swap r@  apply-rule?
    IF  r> apply-rule! true  ELSE  r> drop 2drop false  THEN ;

\ player may move up, down, left and right

:m move:  ( offset -- )
    Create ,
    DOES>  @ >r  1 moves +!
	S" @ "  S"  @"  r@ play-rule  IF  r> soko +!  EXIT  THEN
	S" @."  S"  &"  r@ play-rule  IF  r> soko +!  EXIT  THEN
	S" @$ " S"  @$" r@ play-rule  IF  r> soko +!  EXIT  THEN
	S" @*." S"  &*" r@ play-rule  IF  r> soko +!  EXIT  THEN
	S" @* " S"  &$" r@ play-rule
	      IF  r> soko +!  1 rocks +! -1 score +!  EXIT  THEN
	S" @$." S"  @*" r@ play-rule
	      IF  r> soko +! -1 rocks +!  1 score +!  EXIT  THEN
	S" &*." S" .&*" r@ play-rule  IF  r> soko +!  EXIT  THEN
	S" &$ " S" .@$" r@ play-rule  IF  r> soko +!  EXIT  THEN
	S" & "  S" .@"  r@ play-rule  IF  r> soko +!  EXIT  THEN
	S" &."  S" .&"  r@ play-rule  IF  r> soko +!  EXIT  THEN
	S" &* " S" .&$" r@ play-rule
	      IF  r> soko +!  1 rocks +! -1 score +!  EXIT  THEN
	S" &$." S" .@*" r@ play-rule
	      IF  r> soko +! -1 rocks +!  1 score +!  EXIT  THEN
	-1 moves +!  r> drop  ;

1            move: soko-right
-1           move: soko-left
/maze        move: soko-down
/maze negate move: soko-up

: print-help
    ." Move soko '@' with h, j, k or l key (like vi)" cr
    ." or with vt100 cursor keys." cr ;

: play-loop ( -- )
    BEGIN
	rocks @ 0=
	IF
	    level# @ 1+ level  0= IF  EXIT  THEN
	THEN
	.maze
	key
	CASE
	    [char] ? OF  print-help false  ENDOF

	    [char] h OF  soko-left  false  ENDOF
	    [char] j OF  soko-down  false  ENDOF
	    [char] k OF  soko-up    false  ENDOF
	    [char] l OF  soko-right false  ENDOF

	    \ vt100 cursor keys should work too
	    27       OF  key [char] [ <>   ENDOF
	    [char] D OF  soko-left  false  ENDOF
	    [char] B OF  soko-down  false  ENDOF
	    [char] A OF  soko-up    false  ENDOF
	    [char] C OF  soko-right false  ENDOF

	    \ Game boy cursor keys!
	    k-left   OF  soko-left  false  ENDOF
	    k-down   OF  soko-down  false  ENDOF
	    k-up     OF  soko-up    false  ENDOF
	    k-right  OF  soko-right false  ENDOF

	    [char] q OF  true              ENDOF
	false swap  ENDCASE
    UNTIL ;

\ start game with "sokoban"

: sokoban ( -- )
    page 1 level IF  play-loop ." Game finished!"  THEN ;

: init-variables
    0 soko !
    0 rocks !
    0 level# !
    0 moves !
    0 score ! ;

: main
    install-font
    init-term
    init-input
    init-variables
    sokoban ;

001 new-maze
m:     #####
m:     #   #
m:     #$  #
m:   ###  $##
m:   #  $ $ #
m: ### # ## #   ######
m: #   # ## #####  ..#
m: # $  $          ..#
m: ##### ### #@##  ..#
m:     #     #########
m:     #######
002 new-maze
m: ############
m: #..  #     ###
m: #..  # $  $  #
m: #..  #$####  #
m: #..    @ ##  #
m: #..  # #  $ ##
m: ###### ##$ $ #
m:   # $  $ $ $ #
m:   #    #     #
m:   ############
003 new-maze
m:         ########
m:         #     @#
m:         # $#$ ##
m:         # $  $#
m:         ##$ $ #
m: ######### $ # ###
m: #....  ## $  $  #
m: ##...    $  $   #
m: #....  ##########
m: ########
004 new-maze
m:            ########
m:            #  ....#
m: ############  ....#
m: #    #  $ $   ....#
m: # $$$#$  $ #  ....#
m: #  $     $ #  ....#
m: # $$ #$ $ $########
m: #  $ #     #
m: ## #########
m: #    #    ##
m: #     $   ##
m: #  $$#$$  @#
m: #    #    ##
m: ###########
005 new-maze
m:         #####
m:         #   #####
m:         # #$##  #
m:         #     $ #
m: ######### ###   #
m: #....  ## $  $###
m: #....    $ $$ ##
m: #....  ##$  $ @#
m: #########  $  ##
m:         # $ $  #
m:         ### ## #
m:           #    #
m:           ######
006 new-maze
m: ######  ###
m: #..  # ##@##
m: #..  ###   #
m: #..     $$ #
m: #..  # # $ #
m: #..### # $ #
m: #### $ #$  #
m:    #  $# $ #
m:    # $  $  #
m:    #  ##   #
m:    #########
007 new-maze
m:        #####
m:  #######   ##
m: ## # @## $$ #
m: #    $      #
m: #  $  ###   #
m: ### #####$###
m: # $  ### ..#
m: # $ $ $ ...#
m: #    ###...#
m: # $$ # #...#
m: #  ### #####
m: ####
008 new-maze
m:   ####
m:   #  ###########
m:   #    $   $ $ #
m:   # $# $ #  $  #
m:   #  $ $  #    #
m: ### $# #  #### #
m: #@#$ $ $  ##   #
m: #    $ #$#   # #
m: #   $    $ $ $ #
m: #####  #########
m:   #      #
m:   #      #
m:   #......#
m:   #......#
m:   #......#
m:   ########
009 new-maze
m:           #######
m:           #  ...#
m:       #####  ...#
m:       #      . .#
m:       #  ##  ...#
m:       ## ##  ...#
m:      ### ########
m:      # $$$ ##
m:  #####  $ $ #####
m: ##   #$ $   #   #
m: #@ $  $    $  $ #
m: ###### $$ $ #####
m:      #      #
m:      ########
010 new-maze
m:  ###  #############
m: ##@####       #   #
m: # $$   $$  $ $ ...#
m: #  $$$#    $  #...#
m: # $   # $$ $$ #...#
m: ###   #  $    #...#
m: #     # $ $ $ #...#
m: #    ###### ###...#
m: ## #  #  $ $  #...#
m: #  ## # $$ $ $##..#
m: # ..# #  $      #.#
m: # ..# # $$$ $$$ #.#
m: ##### #       # #.#
m:     # ######### #.#
m:     #           #.#
m:     ###############
011 new-maze
m:           ####
m:      #### #  #
m:    ### @###$ #
m:   ##      $  #
m:  ##  $ $$## ##
m:  #  #$##     #
m:  # # $ $$ # ###
m:  #   $ #  # $ #####
m: ####    #  $$ #   #
m: #### ## $         #
m: #.    ###  ########
m: #.. ..# ####
m: #...#.#
m: #.....#
m: #######
012 new-maze
m: ################
m: #              #
m: # # ######     #
m: # #  $ $ $ $#  #
m: # #   $@$   ## ##
m: # #  $ $ $###...#
m: # #   $ $  ##...#
m: # ###$$$ $ ##...#
m: #     # ## ##...#
m: #####   ## ##...#
m:     #####     ###
m:         #     #
m:         #######
013 new-maze
m: ##### ####
m: #...# #  ####
m: #...###  $  #
m: #....## $  $###
m: ##....##   $  #
m: ###... ## $ $ #
m: # ##    #  $  #
m: #  ## # ### ####
m: # $ # #$  $    #
m: #  $ @ $    $  #
m: #   # $ $$ $ ###
m: #  ######  ###
m: # ##    ####
m: ###
014 new-maze
m:    #########
m:   ##   ##  ######
m: ###     #  #    ###
m: #  $ #$ #  #  ... #
m: # # $#@$## # #.#. #
m: #  # #$  #    . . #
m: # $    $ # # #.#. #
m: #   ##  ##$ $ . . #
m: # $ #   #  #$#.#. #
m: ## $  $   $  $... #
m:  #$ ######    ##  #
m:  #  #    ##########
m:  ####
015 new-maze
m:        #######
m:  #######     #
m:  #     # $@$ #
m:  #$$ #   #########
m:  # ###......##   #
m:  #   $......## # #
m:  # ###......     #
m: ##   #### ### #$##
m: #  #$   #  $  # #
m: #  $ $$$  # $## #
m: #   $ $ ###$$ # #
m: #####     $   # #
m:     ### ###   # #
m:       #     #   #
m:       ########  #
m:              ####
016 new-maze
m:    ########
m:    #   #  #
m:    #  $   #
m:  ### #$   ####
m:  #  $  ##$   #
m:  #  # @ $ # $#
m:  #  #      $ ####
m:  ## ####$##     #
m:  # $#.....# #   #
m:  #  $..**. $# ###
m: ##  #.....#   #
m: #   ### #######
m: # $$  #  #
m: #  #     #
m: ######   #
m:      #####
017 new-maze
m: #####
m: #   ##
m: #    #  ####
m: # $  ####  #
m: #  $$ $   $#
m: ###@ #$    ##
m:  #  ##  $ $ ##
m:  # $  ## ## .#
m:  #  #$##$  #.#
m:  ###   $..##.#
m:   #    #.*...#
m:   # $$ #.....#
m:   #  #########
m:   #  #
m:   ####
018 new-maze
m:    ##########
m:    #..  #   #
m:    #..      #
m:    #..  #  ####
m:   #######  #  ##
m:   #            #
m:   #  #  ##  #  #
m: #### ##  #### ##
m: #  $  ##### #  #
m: # # $  $  # $  #
m: # @$  $   #   ##
m: #### ## #######
m:    #    #
m:    ######
019 new-maze
m:      ###########
m:      #  .  #   #
m:      # #.    @ #
m:  ##### ##..# ####
m: ##  # ..###     ###
m: # $ #...   $ #  $ #
m: #    .. ##  ## ## #
m: ####$##$# $ #   # #
m:   ## #    #$ $$ # #
m:   #  $ # #  # $## #
m:   #               #
m:   #  ###########  #
m:   ####         ####
020 new-maze
m:   ######
m:   #   @####
m: ##### $   #
m: #   ##    ####
m: # $ #  ##    #
m: # $ #  ##### #
m: ## $  $    # #
m: ## $ $ ### # #
m: ## #  $  # # #
m: ## # #$#   # #
m: ## ###   # # ######
m: #  $  #### # #....#
m: #    $    $   ..#.#
m: ####$  $# $   ....#
m: #       #  ## ....#
m: ###################
021 new-maze
m:     ##########
m: #####        ####
m: #     #   $  #@ #
m: # #######$####  ###
m: # #    ## #  #$ ..#
m: # # $     #  #  #.#
m: # # $  #     #$ ..#
m: # #  ### ##     #.#
m: # ###  #  #  #$ ..#
m: # #    #  ####  #.#
m: # #$   $  $  #$ ..#
m: #    $ # $ $ #  #.#
m: #### $###    #$ ..#
m:    #    $$ ###....#
m:    #      ## ######
m:    ########
022 new-maze
m: #########
m: #       #
m: #       ####
m: ## #### #  #
m: ## #@##    #
m: # $$$ $  $$#
m: #  # ## $  #
m: #  # ##  $ ####
m: ####  $$$ $#  #
m:  #   ##   ....#
m:  # #   # #.. .#
m:  #   # # ##...#
m:  ##### $  #...#
m:      ##   #####
m:       #####
023 new-maze
m: ######     ####
m: #    #######  #####
m: #   $#  #  $  #   #
m: #  $  $  $ # $ $  #
m: ##$ $   # @# $    #
m: #  $ ########### ##
m: # #   #.......# $#
m: # ##  # ......#  #
m: # #   $........$ #
m: # # $ #.... ..#  #
m: #  $ $####$#### $#
m: # $   ### $   $  ##
m: # $     $ $  $    #
m: ## ###### $ ##### #
m: #         #       #
m: ###################
024 new-maze
m:     #######
m:     #  #  ####
m: ##### $#$ #  ##
m: #.. #  #  #   #
m: #.. # $#$ #  $####
m: #.  #     #$  #  #
m: #..   $#  # $    #
m: #..@#  #$ #$  #  #
m: #.. # $#     $#  #
m: #.. #  #$$#$  #  ##
m: #.. # $#  #  $#$  #
m: #.. #  #  #   #   #
m: ##. ####  #####   #
m:  ####  ####   #####
025 new-maze
m: ###############
m: #..........  .####
m: #..........$$.#  #
m: ###########$ #   ##
m: #      $  $     $ #
m: ## ####   #  $ #  #
m: #      #   ##  # ##
m: #  $#  # ##  ### ##
m: # $ #$###    ### ##
m: ###  $ #  #  ### ##
m: ###    $ ## #  # ##
m:  # $  #  $  $ $   #
m:  #  $  $#$$$  #   #
m:  #  #  $      #####
m:  # @##  #  #  #
m:  ##############
026 new-maze
m: ####
m: #  ##############
m: #  #   ..#......#
m: #  # # ##### ...#
m: ##$#    ........#
m: #   ##$######  ####
m: # $ #     ######@ #
m: ##$ # $   ######  #
m: #  $ #$$$##       #
m: #      #    #$#$###
m: # #### #$$$$$    #
m: # #    $     #   #
m: # #   ##        ###
m: # ######$###### $ #
m: #        #    #   #
m: ##########    #####
027 new-maze
m:  #######
m:  #  #  #####
m: ##  #  #...###
m: #  $#  #...  #
m: # $ #$$ ...  #
m: #  $#  #... .#
m: #   # $########
m: ##$       $ $ #
m: ##  #  $$ #   #
m:  ######  ##$$@#
m:       #      ##
m:       ########
028 new-maze
m:  #################
m:  #...   #    #   ##
m: ##.....  $## # #$ #
m: #......#  $  #    #
m: #......#  #  # #  #
m: ######### $  $ $  #
m:   #     #$##$ ##$##
m:  ##   $    # $    #
m:  #  ## ### #  ##$ #
m:  # $ $$     $  $  #
m:  # $    $##$ ######
m:  #######  @ ##
m:        ######
029 new-maze
m:          #####
m:      #####   #
m:     ## $  $  ####
m: ##### $  $ $ ##.#
m: #       $$  ##..#
m: #  ###### ###.. #
m: ## #  #    #... #
m: # $   #    #... #
m: #@ #$ ## ####...#
m: ####  $ $$  ##..#
m:    ##  $ $  $...#
m:     # $$  $ #  .#
m:     #   $ $  ####
m:     ######   #
m:          #####
030 new-maze
m: #####
m: #   ##
m: # $  #########
m: ## # #       ######
m: ## #   $#$#@  #   #
m: #  #      $ #   $ #
m: #  ### ######### ##
m: #  ## ..*..... # ##
m: ## ## *.*..*.* # ##
m: # $########## ##$ #
m: #  $   $  $    $  #
m: #  #   #   #   #  #
m: ###################
031 new-maze
m:        ###########
m:        #   #     #
m: #####  #     $ $ #
m: #   ##### $## # ##
m: # $ ##   # ## $  #
m: # $  @$$ # ##$$$ #
m: ## ###   # ##    #
m: ## #   ### #####$#
m: ## #     $  #....#
m: #  ### ## $ #....##
m: # $   $ #   #..$. #
m: #  ## $ #  ##.... #
m: #####   ######...##
m:     #####    #####
032 new-maze
m:   ####
m:   #  #########
m:  ##  ##  #   #
m:  #  $# $@$   ####
m:  #$  $  # $ $#  ##
m: ##  $## #$ $     #
m: #  #  # #   $$$  #
m: # $    $  $## ####
m: # $ $ #$#  #  #
m: ##  ###  ###$ #
m:  #  #....     #
m:  ####......####
m:    #....####
m:    #...##
m:    #...#
m:    #####
033 new-maze
m:       ####
m:   #####  #
m:  ##     $#
m: ## $  ## ###
m: #@$ $ # $  #
m: #### ##   $#
m:  #....#$ $ #
m:  #....#   $#
m:  #....  $$ ##
m:  #... # $   #
m:  ######$ $  #
m:       #   ###
m:       #$ ###
m:       #  #
m:       ####
034 new-maze
m: ############
m: ##     ##  #
m: ##   $   $ #
m: #### ## $$ #
m: #   $ #    #
m: # $$$ # ####
m: #   # # $ ##
m: #  #  #  $ #
m: # $# $#    #
m: #   ..# ####
m: ####.. $ #@#
m: #.....# $# #
m: ##....#  $ #
m: ###..##    #
m: ############
035 new-maze
m:  #########
m:  #....   ##
m:  #.#.#  $ ##
m: ##....# # @##
m: # ....#  #  ##
m: #     #$ ##$ #
m: ## ###  $    #
m:  #$  $ $ $#  #
m:  # #  $ $ ## #
m:  #  ###  ##  #
m:  #    ## ## ##
m:  #  $ #  $  #
m:  ###$ $   ###
m:    #  #####
m:    ####
(
036 new-maze
m: ############ ######
m: #   #    # ###....#
m: #   $$#   @  .....#
m: #   # ###   # ....#
m: ## ## ###  #  ....#
m:  # $ $     # # ####
m:  #  $ $##  #      #
m: #### #  #### # ## #
m: #  # #$   ## #    #
m: # $  $  # ## #   ##
m: # # $ $    # #   #
m: #  $ ## ## # #####
m: # $$     $$  #
m: ## ## ### $  #
m:  #    # #    #
m:  ###### ######
037 new-maze
m:             #####
m: #####  ######   #
m: #   ####  $ $ $ #
m: # $   ## ## ##  ##
m: #   $ $     $  $ #
m: ### $  ## ##     ##
m:   # ##### #####$$ #
m:  ##$##### @##     #
m:  # $  ###$### $  ##
m:  # $  #   ###  ###
m:  # $$ $ #   $$ #
m:  #     #   ##  #
m:  #######.. .###
m:     #.........#
m:     #.........#
m:     ###########
038 new-maze
m: ###########
m: #......   #########
m: #......   #  ##   #
m: #..### $    $     #
m: #... $ $ #   ##   #
m: #...#$#####    #  #
m: ###    #   #$  #$ #
m:   #  $$ $ $  $##  #
m:   #  $   #$#$ ##$ #
m:   ### ## #    ##  #
m:    #  $ $ ## ######
m:    #    $  $  #
m:    ##   # #   #
m:     #####@#####
m:         ###
039 new-maze
m:       ####
m: ####### @#
m: #     $  #
m: #   $## $#
m: ##$#...# #
m:  # $...  #
m:  # #. .# ##
m:  #   # #$ #
m:  #$  $    #
m:  #  #######
m:  ####
040 new-maze
m:              ######
m:  #############....#
m: ##   ##     ##....#
m: #  $$##  $ @##....#
m: #      $$ $#  ....#
m: #  $ ## $$ # # ...#
m: #  $ ## $  #  ....#
m: ## ##### ### ##.###
m: ##   $  $ ##   .  #
m: # $###  # ##### ###
m: #   $   #       #
m: #  $ #$ $ $###  #
m: # $$$# $   # ####
m: #    #  $$ #
m: ######   ###
m:      #####
041 new-maze
m:     ############
m:     #          ##
m:     #  # #$$ $  #
m:     #$ #$#  ## @#
m:    ## ## # $ # ##
m:    #   $ #$  # #
m:    #   # $   # #
m:    ## $ $   ## #
m:    #  #  ##  $ #
m:    #    ## $$# #
m: ######$$   #   #
m: #....#  ########
m: #.#... ##
m: #....   #
m: #....   #
m: #########
042 new-maze
m:            #####
m:           ##   ##
m:          ##     #
m:         ##  $$  #
m:        ## $$  $ #
m:        # $    $ #
m: ####   #   $$ #####
m: #  ######## ##    #
m: #.            $$$@#
m: #.# ####### ##   ##
m: #.# #######. #$ $##
m: #........... #    #
m: ##############  $ #
m:              ##  ##
m:               ####
043 new-maze
m:      ########
m:   ####      ######
m:   #    ## $ $   @#
m:   # ## ##$#$ $ $##
m: ### ......#  $$ ##
m: #   ......#  #   #
m: # # ......#$  $  #
m: # #$...... $$# $ #
m: #   ### ###$  $ ##
m: ###  $  $  $  $ #
m:   #  $  $  $  $ #
m:   ######   ######
m:        #####
044 new-maze
m:         #######
m:     #####  #  ####
m:     #   #   $    #
m:  #### #$$ ## ##  #
m: ##      # #  ## ###
m: #  ### $#$  $  $  #
m: #...    # ##  #   #
m: #...#    @ # ### ##
m: #...#  ###  $  $  #
m: ######## ##   #   #
m:           #########
045 new-maze
m:  #####
m:  #   #
m:  # # #######
m:  #      $@######
m:  # $ ##$ ###   #
m:  # #### $    $ #
m:  # ##### #  #$ ####
m: ##  #### ##$      #
m: #  $#  $  # ## ## #
m: #         # #...# #
m: ######  ###  ...  #
m:      #### # #...# #
m:           # ### # #
m:           #       #
m:           #########
046 new-maze
m: ##########
m: #        ####
m: # ###### #  ##
m: # # $ $ $  $ #
m: #       #$   #
m: ###$  $$#  ###
m:   #  ## # $##
m:   ##$#   $ @#
m:    #  $ $ ###
m:    # #   $  #
m:    # ##   # #
m:   ##  ##### #
m:   #         #
m:   #.......###
m:   #.......#
m:   #########
047 new-maze
m:          ####
m:  #########  ##
m: ##  $      $ #####
m: #   ## ##   ##...#
m: # #$$ $ $$#$##...#
m: # #   @   #   ...#
m: #  $# ###$$   ...#
m: # $  $$  $ ##....#
m: ###$       #######
m:   #  #######
m:   ####
048 new-maze
m:   #########
m:   #*.*#*.*#
m:   #.*.*.*.#
m:   #*.*.*.*#
m:   #.*.*.*.#
m:   #*.*.*.*#
m:   ###   ###
m:     #   #
m: ###### ######
m: #           #
m: # $ $ $ $ $ #
m: ## $ $ $ $ ##
m:  #$ $ $ $ $#
m:  #   $@$   #
m:  #  #####  #
m:  ####   ####
049 new-maze
m:        ####
m:        #  ##
m:        #   ##
m:        # $$ ##
m:      ###$  $ ##
m:   ####    $   #
m: ###  # #####  #
m: #    # #....$ #
m: # #   $ ....# #
m: #  $ # #.*..# #
m: ###  #### ### #
m:   #### @$  ##$##
m:      ### $     #
m:        #  ##   #
m:        #########
050 new-maze
m:       ############
m:      ##..    #   #
m:     ##..* $    $ #
m:    ##..*.# # # $##
m:    #..*.# # # $  #
m: ####...#  #    # #
m: #  ## #          #
m: # @$ $ ###  #   ##
m: # $   $   # #   #
m: ###$$   # # # # #
m:   #   $   # # #####
m:   # $# #####      #
m:   #$   #   #    # #
m:   #  ###   ##     #
m:   #  #      #    ##
m:   ####      ######
051 new-maze
m: #########
m: #       #
m: #  $   $#
m: ####    #
m:    # $  ##
m: ####   $ #
m: #.. $ ## ####
m: #..  $##    #
m: #..    $    #
m: #.###$### #@#
m: #.# #     ###
m: ### #######
052 new-maze
m: ####################
m: #  ##########     @#
m: # $#    #     ######
m: #      ####   #  ###
m: #####         #  ###
m: #   $         #  ###
m: #  $####  #   #    #
m: # # #  #..#$###  # #
m: # # #$ #..#  $  $$ #
m: #      #..#  #   # #
m: #   #  #..#  #   # #
m: ####################
053 new-maze
m: ####################
m: #                ###
m: # $#   $ ##  $    ##
m: #    $###    # $$ ##
m: #.###     $ $ ##  ##
m: #...#  #  #    #$  #
m: #..##$$#### $  #   #
m: #...#      $ ##  ###
m: #...$  ###  #    # #
m: ##..  $#  ##   ##@ #
m: ###.#              #
m: ####################
054 new-maze
m: ####################
m: #   #    #   #   #@#
m: # $      $   $   # #
m: ## ###..## ###     #
m: #   #....#$#  $### #
m: # $ #....#  $  $ $ #
m: #   #....# # # $ $ #
m: #   ##..##   #$#   #
m: ##$##    ##  #  #$##
m: #   $  $     #  #  #
m: #   #    #   #     #
m: ####################
055 new-maze
m: ####################
m: #    @##      #   ##
m: #    ##    $    $ ##
m: #  ###....# # #  ###
m: #   #....# # # $   #
m: ### #...#  #       #
m: ##  ##.#     $   $ #
m: ##  $ $ ###  # # ###
m: ## $       # # $   #
m: #### $  $# # # # $ #
m: ####         # #  ##
m: ####################
056 new-maze
m: ####################
m: #  #  ##    #   @###
m: ##    $    # $###  #
m: ##$# $ ##$# $ $    #
m: #   $#    $      ###
m: # ##   $ ###  #....#
m: # # $# # # # #....##
m: #    $ $ #  #....###
m: ##$ ###  $ #....####
m: #  # $        ######
m: #      # #    ######
m: ####################
057 new-maze
m: ####################
m: #@     ###   #  #  #
m: # # #  #  $  $     #
m: #####     # $ $#$# #
m: #.#..#    ##$ $    #
m: #.....    $   #   ##
m: #.....    ###$##$###
m: #.#..#    $    #   #
m: #####     #  #$  $ #
m: #####  #  $    $ $ #
m: #####  #  #  #  #  #
m: ####################
058 new-maze
m: ####################
m: ##...   ## #    #  #
m: #....         $ ## #
m: #....# # #$###$    #
m: #...#    #       # #
m: ##.#  #$ #     $## #
m: #  #  # $ $ ###  $ #
m: #     $  $ #  # ## #
m: ## # ## #$$# $#  # #
m: #  #   $ $ #      ##
m: #    #     #  #   @#
m: ####################
059 new-maze
m: ####################
m: #   #  #@# ##  #####
m: # # #  $    $  #####
m: # #    ###### $  ###
m: #   #  #....#  $$  #
m: ##$##$##....#      #
m: #      #....##$##$##
m: #  $$  #....#      #
m: # $  $  #  #  ###  #
m: #####  $   $    $  #
m: ##### #    #  #   ##
m: ####################
060 new-maze
m: ####################
m: #     ###..###     #
m: # $$  ###..###  $@ #
m: #  # ##......#  $  #
m: #     #......#  $  #
m: ####  ###..######$ #
m: #   $$$ #..#    #  #
m: # $#   $  $  $$ #$ #
m: #  #  ## $  ##  #  #
m: # $    $ ## $    $ #
m: #  #  ##    ##  #  #
m: ####################
061 new-maze
m: ####################
m: #    #  # #  #  #  #
m: # @# # ## $   $   ##
m: #### #    #  # $   #
m: #    # ## #$ ## ## #
m: #      $   $   $   #
m: #..###$$## $##$ ## #
m: #..#.#  # $   $ #  #
m: #....# $$   ##$ ####
m: #....#  #####      #
m: #...###        ##  #
m: ####################
062 new-maze
m: ####################
m: #....#       #  #  #
m: #....# # $  $      #
m: #.... ##  $# # $#$ #
m: #...#   $   $#  $  #
m: #..####  # $   $$  #
m: #      #### #### ###
m: #        #   #     #
m: # ##   #   $ # $ $ #
m: # ##    $ ## $  $  #
m: #     @#     #   # #
m: ####################
063 new-maze
m: ####################
m: #....###           #
m: #....##### #  #$# ##
m: #....###   #$  $   #
m: #....###    $  #$$##
m: ##  #### $#  #$ $  #
m: ##  ####  $  $  #  #
m: #@  ####$###$## $  #
m: ##        #  #  $  #
m: ##   ###  #  $  ####
m: ########  #  #     #
m: ####################
064 new-maze
m: ####################
m: #     #     @#...###
m: #     #      ##...##
m: # # # ##$## ## ....#
m: #   $ #   $$$  ....#
m: ###$### $$  ### ##.#
m: #     $  #    # ####
m: #  $  #  ###  # #  #
m: ## #$##    $  $$   #
m: #   $ ##   #  # #  #
m: #     #    #  #    #
m: ####################
065 new-maze
m: ####################
m: #     #  #...#@    #
m: # #       ....#    #
m: #  $  #   #....#   #
m: # ##$#### ##....#  #
m: # $   $  #  #...#  #
m: # $$ #   #   # $$  #
m: ###  $$$#   $$  $  #
m: # $  #  #    # $#  #
m: #   $#  #       $  #
m: #  #    #    #  #  #
m: ####################
066 new-maze
m: ####################
m: #####@###.##...##  #
m: #####$  ..#...#    #
m: ####    ......#  $ #
m: ###  $ #.....## # ##
m: ##  $$# #####  $ $ #
m: ## $# $    ##  $$  #
m: ##  #  #    # $  $ #
m: ##   $$ ### #$##   #
m: ## $#      $ $  $ ##
m: ###    #    #    ###
m: ####################
067 new-maze
m: ####################
m: #@     #   #       #
m: ## ### ##  #### # ##
m: #    # #  $$       #
m: #  # # # $ # $ ## ##
m: #     $ #  #$$ #   #
m: #  ###  #      ## ##
m: #..#.# $ #  $ #    #
m: #..#.#  $ # ## $$  #
m: #....##   $$  $  # #
m: #.....##        #  #
m: ####################
068 new-maze
m: ####################
m: #  #      #   #   ##
m: # $# $ $ ##...$  $ #
m: #  $  # ##....# $  #
m: # ## $ ##....#   $ #
m: # $    #....## $   #
m: # $##  #...#       #
m: #   $$$##$##  ### ##
m: # # #  #   #  #    #
m: # $ #  $  ##       #
m: #    #    #@       #
m: ####################
069 new-maze
m: ####################
m: #  #  # #    #  #  #
m: #   $      $ $     #
m: ## #  #$###$##  ## #
m: #   $     $  #  $  #
m: # ###$##$#   # $   #
m: # #   $ $  ###### $#
m: # $  $$ $  #@#.#...#
m: # #     #  # #.#...#
m: # ########## #.....#
m: #            #.....#
m: ####################
070 new-maze
m: ####################
m: #  #     #  ##    ##
m: # $#   $ #     ##  #
m: # $  $  #..#     $ #
m: # $ $  #....#   # ##
m: # $#  #......### $ #
m: #   #  #....#  #$  #
m: # $  ####..#   #   #
m: ## $   ## # # $  $##
m: ### $    $#@$ $#   #
m: ####   #       #   #
m: ####################
071 new-maze
m: ####################
m: #      ....#    ####
m: #      ....        #
m: # # ##########     #
m: # #$   #      ###..#
m: #  $   #$$###   #..#
m: # $ ### $   $   #..#
m: # $ #   $ $ #  ##..#
m: #  #  $$ # $ ##   ##
m: #@## $#  $  $     ##
m: ##       ##   #  ###
m: ####################
072 new-maze
m: ####################
m: #        #   #@ #  #
m: # $$  #$$# # #  ## #
m: #  # $ $ #$$ #     #
m: ## #  #  # # #  #  #
m: #   ##       #     #
m: #   # $ #   #   #  #
m: # $ #$ #   #  $ #..#
m: ##$ #  ####    #...#
m: #  $          #....#
m: #   #  #     #.....#
m: ####################
073 new-maze
m: ####################
m: #     #   #####    #
m: ## $  #   ####  $  #
m: #### $$   #..#  #  #
m: #  $  $  ##..#### ##
m: # $   ###....   $$ #
m: #  #$#   ....# # $ #
m: # #  # $ ..###$#   #
m: # #   $ #..#   ##  #
m: #   $#  ####   # $##
m: # #  #    @#      ##
m: ####################
074 new-maze
m: ####################
m: #   #   #    #   #@#
m: #   $  $     # $ # #
m: ##$# $### #    $$# #
m: #  #  #.###  #$ $  #
m: #  #$#....#  # ### #
m: # $  #.....##    # #
m: ##$  #.#....#$$ $  #
m: #  ######..## #  # #
m: #  $         $ ### #
m: #   #   #        # #
m: ####################
075 new-maze
m: ####################
m: # # # #   #@##   # #
m: #             $    #
m: #  ##$# ##### $ # ##
m: ##    ##.....#  #  #
m: ##$##$#.....###$#$ #
m: #   # ##.....#  # ##
m: #  $    ##..##  #  #
m: # $ #   $   $  $$$ #
m: ## $  $# #  #  $   #
m: #   ##   #  #      #
m: ####################
076 new-maze
m: ######  #####
m: #    #  #   #
m: # $  #### $ #
m: # $      $  #
m: #  ###@###$ #
m: ########## ###
m: #..   ##     #
m: #..   ##$    #
m: #..   ## $   #
m: #..   ## $   #
m: #..     $ $  #
m: ###  #########
m:   ####
077 new-maze
m:        ###########
m:        #         #
m:        #    $ $  #
m: ###### # $ ##### #
m: #    ##### $  ##$#
m: #       $ $      #
m: #          ## ## #
m: #    ##@##### ## #
m: #    ####   # ## ##
m: #....#      # $   #
m: #....#      #     #
m: ######      #######
078 new-maze
m: #############
m: #           #
m: # ### $$    #
m: #   # $  $  #
m: #  $####$######
m: # $ ##        #####
m: #  $$ $        ...#
m: ### ## $$#     ...#
m:   # ##   #     ...#
m:   #      #     ...#
m:   ###@#############
m:     ###
079 new-maze
m:   #################
m: ###@##         ...#
m: #    #         ...#
m: # $  #         ...#
m: # $$ #         ...#
m: ## $ ###$##########
m:  # ###  $ #
m: ##   $  $ #
m: #  $ #  $ #
m: # $  #    #
m: #  $ #    #
m: #    #    #
m: ###########
080 new-maze
m:               #####
m:      ##########   #
m:      #        #   #
m:      #  $ $    $$ #
m:      # ##### ## $ #
m:      #$$   #$## $ #
m:      # ### # ##$  #
m: ###### ### $ $    #
m: #....        ##   #
m: #....        ######
m: #....        #
m: ###########@##
m:           ###
081 new-maze
m:     ######
m:  ####    #
m:  #    ## #
m:  # $     #
m: ### #### ########
m: #  $   $ ##  ...#
m: #   $$ $$    ...#
m: #    $  $##  ...#
m: ##@## ## ##  ...#
m:  ###  $  ########
m:  #   $$  #
m:  #    #  #
m:  #########
082 new-maze
m: ####### #########
m: #     # #   ##  #
m: # ### # #   $   #
m: # # $ ###   $   #
m: #   $$      ##$ #
m: #    ####   ##  #
m: #@############ ##
m: ###..    #####$ #
m:   #..    ####   #
m:   #..       $$  #
m:   #..    #### $ #
m:   #..    #  #   #
m:   ########  #####
083 new-maze
m: #######
m: #     ##########
m: #     #    #  ##
m: # $   #   $ $  #
m: #  $  #  $ ##  #
m: # $$  ##$ $    #
m: ## #  ## #######
m: ## #  ##    ...#
m: #  #$       ...#
m: #   $$      ...#
m: #     ##@#  ...#
m: ################
084 new-maze
m: ############
m: #      #   ##
m: # $  $   #  ######
m: ####  #####      #
m:  #..  #     #### #
m:  #.####  ####    #
m:  #....    #  $ ####
m:  # ...#   # $$$#  ##
m: ###.#### ##  $@$   #
m: #     ##### $ #    #
m: # #.# $      $###$ #
m: # #.########  #  $ #
m: # #..        ##  $ #
m: # # ####### $ # #  #
m: #   #     #       ##
m: #####     ##########
085 new-maze
m: ####################
m: # #     #          #
m: #       $  ## ### ##
m: #####  ##   $  $   #
m: ##..##  # # $ # #  #
m: #....  $     ##$# ##
m: #....  $#####   #$##
m: ##..# #  #   #  $  #
m: ###.# #  $   $  # @#
m: ##  $  $ #   #  ####
m: ##       ###########
m: ####################
)
