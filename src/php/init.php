<?php
    define('REDIS_CONFIG', 'redis-host');
    define('REDIS_PREFIX', 'vc:');
    define('USER_IP', isset($_SERVER['HTTP_CF_CONNECTING_IP']) ? $_SERVER['HTTP_CF_CONNECTING_IP'] : (isset($_SERVER['REMOTE_ADDR']) ? $_SERVER['REMOTE_ADDR'] : "127.0.0.1"));

    if(!isset($_COOKIE["VC:UID"])) {
        setcookie("VC:UID", uniqid());
    }

    spl_autoload_register(function ($class_name) {
        include dirname(__FILE__) . '/' . strtolower($class_name) . '.php';
    });
?>