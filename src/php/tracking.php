<?php
    class TrackingEvent {

    }

    class Tracking {
        public static function CreateEventFromDownload(){
            return new TrackingEvent();
        }

        public function TrackDownload($ev) {

        }
    }
?>