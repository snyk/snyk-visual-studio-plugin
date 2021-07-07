test('should set success to OK upon success', function () {
    // GIVEN
    comp.password = comp.confirmPassword = 'myPassword';

    // WHEN
    comp.changePassword();

    // THEN
    expect(comp.doNotMatch).toBeNull();
    expect(comp.error).toBeNull();
    expect(comp.success).toBe('OK');
});

fs.readFile('backup.txt', 'ascii', function (err, data) {
    if (!err) {
        data = data;
    }
});

var time = t.slice(reminder + remindToken.length);
time = time.replace(/\n$/, '');