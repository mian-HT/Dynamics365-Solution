package com.xgate.d365proxy;

import org.mybatis.spring.annotation.MapperScan;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.cache.annotation.EnableCaching;

@EnableCaching
@MapperScan("com.xgate.d365proxy.mapper")
@SpringBootApplication
public class D365ProxyApplication {

    public static void main(String[] args) {
        SpringApplication.run(D365ProxyApplication.class, args);
    }
}
