// Tools here:

/**
 * @description 核心防抖函数
 * @param {Function} fn 真正要执行的业务函数
 * @param {Number} delay 延迟等待时间(毫米), 默认 500ms
 * @returns {Function} 返回一个具备防抖能力的新函数
 */
export function debounce(fn, delay = 500) {
    let timer = null

    return function (...args) {
        // 如果在延迟时间内再次触发，就忽略上一次的计时器
        if(timer) clearTimeout(timer)

        //重新开始倒计时
        timer = setTimeout(() => {
            //绑定this和参数，防止上下文丢失
            fn.apply(this, args)
        }, delay)
    }
}


/**
 * @description 核心节流函数
 * @param {Function} fn 真正要执行的业务函数
 * @param {Number} delay 冷却时间(毫米), 默认 1000ms
 * @returns {Function} 返回一个具备节流冷却时间的新函数
 */
export function throttle(fn, delay = 1000) {
    let lastTime = 0 //记录上一次真正执行的时间戳

    return function (...args) {
        const now = Date.now() //获取当前的时间戳

        //如果 当前时间 - 上次执行时间 > 冷却时间 , 才允许执行
        if (now - lastTime >= delay) {
            fn.apply(this, args)
            lastTime = now //执行结束后, 更新上一次执行时间, 进入新一轮cd
        }
    }
}
