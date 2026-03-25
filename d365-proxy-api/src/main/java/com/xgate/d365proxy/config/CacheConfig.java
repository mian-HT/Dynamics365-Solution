package com.xgate.d365proxy.config;

import com.github.benmanes.caffeine.cache.Caffeine;
import org.springframework.cache.CacheManager;
import org.springframework.cache.caffeine.CaffeineCacheManager;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.util.concurrent.TimeUnit;

@Configuration
public class CacheConfig {

    /**
     * OAuth Token 缓存：3500 秒后过期（Token 有效期 3599 秒，提前 99 秒刷新）
     */
    @Bean
    public CacheManager cacheManager() {
        CaffeineCacheManager manager = new CaffeineCacheManager("oauthToken");
        manager.setCaffeine(Caffeine.newBuilder()
                .expireAfterWrite(3500, TimeUnit.SECONDS)
                .maximumSize(200));
        return manager;
    }
}
