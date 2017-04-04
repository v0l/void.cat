<?php
        function formatSizeUnits($bytes)
        {
                if ($bytes >= 1073741824)
                {
                        $bytes = number_format($bytes / 1073741824, 2) . ' GB';
                }
                elseif ($bytes >= 1048576)
                {
                        $bytes = number_format($bytes / 1048576, 2) . ' MB';
                }
                elseif ($bytes >= 1024)
                {
                        $bytes = number_format($bytes / 1024, 2) . ' kB';
                }
                elseif ($bytes > 1)
                {
                        $bytes = $bytes . ' bytes';
                }
                elseif ($bytes == 1)
                {
                        $bytes = $bytes . ' byte';
                }
                else
                {
                        $bytes = '0 bytes';
                }

                return $bytes;
        }

        $size = filesize($f->path);
?>
<div id="stats">
        <div class="header">Views: <?php echo $f->views; ?> <font style="float: right">Size: <?php echo formatSizeUnits($size); ?></font></div>
</div>
