\ Copyright (c) 2024 Travis Bemann
\
\ Permission is hereby granted, free of charge, to any person obtaining a copy
\ of this software and associated documentation files (the "Software"), to deal
\ in the Software without restriction, including without limitation the rights
\ to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
\ copies of the Software, and to permit persons to whom the Software is
\ furnished to do so, subject to the following conditions:
\ 
\ The above copyright notice and this permission notice shall be included in
\ all copies or substantial portions of the Software.
\ 
\ THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
\ IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
\ FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
\ AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
\ LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
\ OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
\ SOFTWARE.

begin-module zscript-init-fat32-tool

  zscript-oo import
  zscript-fat32 import
  zscript-block-dev import

  begin-module zscript-init-fat32-tool-internal
    
    \ Sector size
    zscript-fat32-internal::sector-size constant sector-size
    
    \ Do an alignment-safe 32-bit store
    : unaligned!+ { x offset seq -- }
      x offset seq c!+
      x 8 rshift offset 1+ seq c!+
      x 16 rshift offset 2 + seq c!+
      x 24 rshift offset 3 + seq c!+
    ;

    \ Do an alignment-safe 16-bit store
    : unaligned-h!+ { h offset seq -- }
      h offset seq c!+
      h 8 rshift offset 1+ seq c!+
    ;

    \ Clear scratchpad
    : clear-scratchpad { scratchpad -- }
      sector-size 0 ?do 0 i scratchpad c!+ loop
    ;
    
    \ Populate a partition entry
    : init-partition { media -- partition }
      active-partition fat32-lba-partition-type 1 media block-count 1-
      make-partition
    ;

    \ Get the number of sectors for data for media
    \ x + 2((x / cluster-sectors) / 128) = total
    \ x + ((x / cluster-sectors) / 64) = total
    \ 64x + (x / cluster-sectors ) = 64total
    \ (64 * cluster-sectors)x + x = (64 * cluster-sectors)total
    \ ((64 * cluster-sectors) + 1)x = (64 * cluster-sectors)total
    \ x = (64 * cluster-sectors)total / ((64 * cluster-sectors) + 1)
    : data-sectors { cluster-sectors media -- sectors }
      media block-count 3 - \ Exclude MBR, first partition, and info sectors
      64 cluster-sectors * *
      64 cluster-sectors * 1+ /
    ;

    \ Get the number of sectors for FAT for media
    : fat-sectors { cluster-sectors media -- sectors }
      cluster-sectors media data-sectors cluster-sectors / 128 align 128 /
    ;

    \ Populate a FAT32 filesystem's VBR
    : init-vbr { cluster-sectors scratchpad media -- }
      scratchpad clear-scratchpad
      512 $00B scratchpad unaligned-h!+ \ Sector size
      cluster-sectors $00D scratchpad c!+ \ Cluster sector count
      2 $00E scratchpad h!+ \ Reserved sector count
      2 $010 scratchpad c!+ \ FAT count
      $F8 $015 scratchpad c!+ \ Media descriptor
      media block-count 1- $020 scratchpad w!+ \ Sector count
      cluster-sectors media fat-sectors $024 scratchpad w!+ \ FAT sectors
      0 $02A scratchpad h!+ \ Filesystem version
      2 $02C scratchpad w!+ \ Root directory cluster
      1 $030 scratchpad h!+ \ Info sector
      $28 $042 scratchpad c!+ \ Extended boot signature
      scratchpad 1 media block!
    ;

    \ Populate a FAT32 filesystem's info sector
    : init-info { cluster-sectors scratchpad media -- }
      scratchpad clear-scratchpad
      $41615252 $000 scratchpad w!+ \ Magic
      $61417272 $1E4 scratchpad w!+ \ magic
      $AA550000 $1FC scratchpad w!+ \ Magic
      cluster-sectors media data-sectors cluster-sectors / $1E8 scratchpad w!+
      \ Free clusters
      -1 $1EC scratchpad w!+ \ Recent allocated cluster, initialized to -1
      scratchpad 2 media block!
    ;

    \ Initialize a FAT
    : init-fat { cluster-sectors first-sector scratchpad media -- }
      scratchpad clear-scratchpad
      $0FFFFFF8 $008 scratchpad w!+ \ First root directory cluster
      scratchpad first-sector media block!
      0 $008 scratchpad w!+
      cluster-sectors media fat-sectors { fat-sectors }
      fat-sectors 1 > if
        fat-sectors first-sector + first-sector 1+ ?do
          scratchpad i media block!
        loop
      then
    ;

    \ Initialize the root directroy
    : init-root-dir { cluster-sectors scratchpad media -- }
      scratchpad clear-scratchpad
      cluster-sectors media fat-sectors 2 * 3 + dup cluster-sectors + swap ?do
        scratchpad i media block!
      loop
    ;
    
    \ Populate a FAT32 filesystem
    : init-fat32 { cluster-sectors scratchpad media -- }
      cluster-sectors scratchpad media init-vbr
      cluster-sectors scratchpad media init-info
      cluster-sectors 3 scratchpad media init-fat
      cluster-sectors dup media fat-sectors 3 + scratchpad media init-fat
      cluster-sectors scratchpad media init-root-dir
    ;

  end-module> import
  
  \ Initialize a FAT32 filesystem in a single partition on a medium
  : init-partition-and-fat32 { cluster-sectors media -- }
    sector-size make-bytes { scratchpad }
    media make-mbr { mbr }
    mbr format-mbr
    media init-partition 0 mbr partition!
    cluster-sectors scratchpad media init-fat32
  ;
  
end-module
